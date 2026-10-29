// Spike tracker: watches Blazor's hidden phases from inside the page. Installed as a Playwright init script, so it runs
// before any page script.
(() => {
    if (window.__blazorTest) {
        return;
    }

    const state = {
        dotNetCalls: new Set(),
        lastBatchReceived: 0,
        lastBatchCompleted: 0,
        serverUpdatesSent: 0,
        serverUpdatesEnded: 0,
        serverRootsAttached: 0,
        lastBatchBeforeAttach: null,
        wasmUpdatesSent: 0,
        wasmUpdatesEnded: 0,
        navigating: false,
        lastActivity: performance.now(),
        failure: null,
        decodeErrors: 0,
    };
    const waiters = new Set();

    function touch() {
        state.lastActivity = performance.now();
        for (const waiter of waiters) {
            waiter();
        }
    }

    function fail(reason) {
        if (!state.failure) {
            state.failure = reason;
        }
        touch();
    }

    // .NET call hook: every DOM event and [JSInvokable] call goes through invokeMethodAsync.

    const modalSelector = '[aria-modal="true"], [role="dialog"], [role="alertdialog"], dialog:modal';

    function openModals() {
        return [...document.querySelectorAll(modalSelector)].filter(element => element.checkVisibility());
    }

    function beginCall(name) {
        const call = { name, modalsAtStart: new Set(openModals()) };
        state.dotNetCalls.add(call);
        touch();
        return call;
    }

    function endCall(call) {
        state.dotNetCalls.delete(call);
        touch();
    }

    // A call that opened a dialog is waiting for the user, not the app, so it doesn't count while that dialog is open.
    function isParked(call, modals) {
        return modals.some(modal => !call.modalsAtStart.has(modal));
    }

    function busyCalls() {
        const modals = openModals();
        return [...state.dotNetCalls].filter(call => !isParked(call, modals));
    }

    function countByName(calls) {
        const counts = {};
        for (const call of calls) {
            counts[call.name] = (counts[call.name] ?? 0) + 1;
        }
        return counts;
    }

    function counted(invoke, nameOf) {
        return function (...args) {
            const call = beginCall(nameOf(args));
            let promise;
            try {
                promise = invoke.apply(this, args);
            } catch (error) {
                endCall(call);
                throw error;
            }
            promise.then(() => endCall(call), () => endCall(call));
            return promise;
        };
    }

    function hookDotNet(dotNet) {
        const prototype = dotNet.DotNetObject.prototype;
        prototype.invokeMethodAsync = counted(prototype.invokeMethodAsync, args => args[0]);
        dotNet.invokeMethodAsync = counted(dotNet.invokeMethodAsync, args => `${args[0]}.${args[1]}`);

        // .NET-to-JS calls: each runtime attaches a dispatcher, and beginInvokeJSFromDotNet settles once the JS function
        // has settled and its result is on its way back to .NET.
        const attachDispatcher = dotNet.attachDispatcher;
        dotNet.attachDispatcher = function (...args) {
            const dispatcher = attachDispatcher.apply(this, args);
            dispatcher.beginInvokeJSFromDotNet = counted(dispatcher.beginInvokeJSFromDotNet, args => `JS ${args[1]}`);
            return dispatcher;
        };
    }

    function hookOnAssign(target, name, onAssign) {
        let value;
        Object.defineProperty(target, name, {
            configurable: true,
            get: () => value,
            set: assigned => {
                value = assigned && onAssign(assigned);
            },
        });
    }

    hookOnAssign(window, 'DotNet', dotNet => {
        hookDotNet(dotNet);
        return dotNet;
    });

    // WebAssembly root components: blazor.web.js calls these when it attaches components to the .NET runtime.
    hookOnAssign(window, 'Blazor', blazor => {
        hookOnAssign(blazor._internal, 'updateRootComponents', update => function (...args) {
            state.wasmUpdatesSent++;
            touch();
            return update.apply(this, args);
        });
        hookOnAssign(blazor._internal, 'endUpdateRootComponents', end => function (...args) {
            state.wasmUpdatesEnded++;
            touch();
            return end.apply(this, args);
        });
        // blazor.web.js adds an enhanced navigation's new root components in a microtask after enhancednavigationend,
        // so the navigation counts until the next task, by which time that update has been sent.
        hookOnAssign(blazor, 'addEventListener', addEventListener => {
            addEventListener('enhancednavigationstart', () => {
                state.navigating = true;
                touch();
            });
            addEventListener('enhancednavigationend', () => setTimeout(() => {
                state.navigating = false;
                touch();
            }));
            return addEventListener;
        });
        return blazor;
    });

    // Circuit hook: decodes SignalR's MessagePack ("blazorpack") framing on the /_blazor WebSocket.

    class MessagePackReader {
        constructor(bytes, position) {
            this.bytes = bytes;
            this.position = position;
        }

        byte() {
            if (this.position >= this.bytes.length) {
                throw new Error('Unexpected end of message.');
            }
            return this.bytes[this.position++];
        }

        unsigned(size) {
            let value = 0;
            for (let i = 0; i < size; i++) {
                value = (value * 256) + this.byte();
            }
            return value;
        }

        arrayLength() {
            const b = this.byte();
            if ((b & 0xf0) === 0x90) return b & 0x0f;
            if (b === 0xdc) return this.unsigned(2);
            if (b === 0xdd) return this.unsigned(4);
            throw new Error(`Expected an array, found 0x${b.toString(16)}.`);
        }

        mapLength() {
            const b = this.byte();
            if ((b & 0xf0) === 0x80) return b & 0x0f;
            if (b === 0xde) return this.unsigned(2);
            if (b === 0xdf) return this.unsigned(4);
            throw new Error(`Expected a map, found 0x${b.toString(16)}.`);
        }

        string() {
            const b = this.byte();
            let length;
            if (b === 0xc0) return null;
            if ((b & 0xe0) === 0xa0) length = b & 0x1f;
            else if (b === 0xd9) length = this.unsigned(1);
            else if (b === 0xda) length = this.unsigned(2);
            else if (b === 0xdb) length = this.unsigned(4);
            else throw new Error(`Expected a string, found 0x${b.toString(16)}.`);
            const text = new TextDecoder().decode(this.bytes.subarray(this.position, this.position + length));
            this.position += length;
            return text;
        }

        integer() {
            const b = this.byte();
            if (b <= 0x7f) return b;
            if (b >= 0xe0) return b - 0x100;
            if (b === 0xcc) return this.unsigned(1);
            if (b === 0xcd) return this.unsigned(2);
            if (b === 0xce) return this.unsigned(4);
            if (b === 0xcf) return this.unsigned(8);
            if (b === 0xd0) return (this.unsigned(1) << 24) >> 24;
            if (b === 0xd1) return (this.unsigned(2) << 16) >> 16;
            if (b === 0xd2) return this.unsigned(4) | 0;
            if (b === 0xd3) {
                const value = this.unsigned(8);
                return value >= 2 ** 63 ? value - (2 ** 64) : value;
            }
            throw new Error(`Expected an integer, found 0x${b.toString(16)}.`);
        }
    }

    // Reads an Invocation message (type 1): [1, headers, invocationId, target, arguments, ...].
    function readInvocation(bytes, start) {
        const reader = new MessagePackReader(bytes, start);
        reader.arrayLength();
        if (reader.integer() !== 1) {
            return null;
        }
        const headerCount = reader.mapLength();
        for (let i = 0; i < headerCount * 2; i++) {
            reader.string();
        }
        reader.string();
        const target = reader.string();
        const argumentCount = reader.arrayLength();
        const firstArgumentIsInteger = argumentCount > 0 && isInteger(bytes[reader.position]);
        return { target, firstArgument: firstArgumentIsInteger ? reader.integer() : null };
    }

    function isInteger(b) {
        return b <= 0x7f || b >= 0xe0 || (b >= 0xcc && b <= 0xcf) || (b >= 0xd0 && b <= 0xd3);
    }

    // Splits a WebSocket payload into messages: each is a VarInt length followed by that many bytes.
    class FrameReader {
        constructor(onInvocation) {
            this.onInvocation = onInvocation;
            this.handshakeDone = false;
        }

        push(data) {
            if (typeof data === 'string') {
                this.handshakeDone ||= data.includes('\x1e');
                return;
            }
            let bytes = data instanceof ArrayBuffer ? new Uint8Array(data) : new Uint8Array(data.buffer, data.byteOffset, data.byteLength);
            if (!this.handshakeDone) {
                const end = bytes.indexOf(0x1e);
                if (end < 0) {
                    return;
                }
                this.handshakeDone = true;
                bytes = bytes.subarray(end + 1);
            }
            try {
                this.readMessages(bytes);
            } catch {
                state.decodeErrors++;
                fail('Could not decode a circuit message, so render batches can no longer be counted.');
            }
        }

        readMessages(bytes) {
            let position = 0;
            while (position < bytes.length) {
                let length = 0;
                let shift = 0;
                let b;
                do {
                    b = bytes[position++];
                    length |= (b & 0x7f) << shift;
                    shift += 7;
                } while (b & 0x80);
                const invocation = readInvocation(bytes.subarray(position, position + length), 0);
                if (invocation) {
                    this.onInvocation(invocation);
                }
                position += length;
            }
        }
    }

    function onServerMessage({ target, firstArgument }) {
        if (target === 'JS.RenderBatch') {
            state.lastBatchReceived = Math.max(state.lastBatchReceived, firstArgument);
        } else if (target === 'JS.EndUpdateRootComponents') {
            state.serverUpdatesEnded++;
        } else if (target === 'JS.AttachComponent') {
            // Both scripts attach roots this way, but only blazor.web.js also receives JS.EndUpdateRootComponents. The
            // root's handlers arrive with the next render batch.
            state.serverRootsAttached++;
            state.lastBatchBeforeAttach = state.lastBatchReceived;
        } else if (target === 'JS.Error') {
            fail('The server reported an unhandled circuit error.');
        }
        touch();
    }

    function onClientMessage({ target, firstArgument }) {
        if (target === 'OnRenderCompleted') {
            state.lastBatchCompleted = Math.max(state.lastBatchCompleted, firstArgument);
            if (state.lastBatchBeforeAttach !== null && state.lastBatchCompleted > state.lastBatchBeforeAttach) {
                state.lastBatchBeforeAttach = null;
            }
        } else if (target === 'UpdateRootComponents') {
            state.serverUpdatesSent++;
        }
        touch();
    }

    function watchCircuit(socket) {
        const incoming = new FrameReader(onServerMessage);
        const outgoing = new FrameReader(onClientMessage);
        socket.addEventListener('message', event => incoming.push(event.data));
        const send = socket.send;
        socket.send = function (data) {
            outgoing.push(data);
            return send.call(this, data);
        };
        socket.addEventListener('close', event => {
            if (!event.wasClean) {
                fail(`The circuit's WebSocket closed with code ${event.code}.`);
            }
        });
    }

    const NativeWebSocket = window.WebSocket;
    window.WebSocket = class extends NativeWebSocket {
        constructor(url, protocols) {
            super(url, protocols);
            if (new URL(url, location.href).pathname.endsWith('/_blazor')) {
                watchCircuit(this);
            }
        }
    };

    // Root component markers: blazor.web.js removes them once it has read them, so they are recorded as the parser
    // inserts them.

    const markerTypes = new Set();
    const seenMarkers = new WeakSet();
    let serverMarkers = 0;

    function recordMarker(comment) {
        // The same comment can show up both as an added node and inside an added element.
        if (seenMarkers.has(comment)) {
            return;
        }
        seenMarkers.add(comment);
        const match = /^\s*Blazor:[^{]*(\{.*\})\s*$/s.exec(comment.textContent);
        if (!match) {
            return;
        }
        try {
            const type = JSON.parse(match[1]).type;
            if (type) {
                markerTypes.add(type);
                serverMarkers += type === 'server' ? 1 : 0;
            }
        } catch {
            state.decodeErrors++;
        }
    }

    function recordMarkers(node) {
        if (node.nodeType === Node.COMMENT_NODE) {
            recordMarker(node);
        } else if (node.nodeType === Node.ELEMENT_NODE && node.hasChildNodes()) {
            const walker = document.createTreeWalker(node, NodeFilter.SHOW_COMMENT);
            while (walker.nextNode()) {
                recordMarker(walker.currentNode);
            }
        }
    }

    // DOM and failure hooks.

    new MutationObserver(records => {
        for (const record of records) {
            record.addedNodes.forEach(recordMarkers);
        }
        const errorUi = document.getElementById('blazor-error-ui');
        if (errorUi && errorUi.style.display === 'block') {
            fail('Blazor showed its error UI.');
        }
        touch();
    }).observe(document, { subtree: true, childList: true, attributes: true, characterData: true });

    document.addEventListener('components-reconnect-state-changed', event => {
        const reconnectState = event.detail?.state;
        if (reconnectState === 'failed' || reconnectState === 'rejected') {
            fail(`The circuit could not reconnect (${reconnectState}).`);
        }
    }, true);

    // Root components that are on the page but not attached yet, by runtime.

    function pendingRootComponents() {
        const pending = [];
        const serverBusy = state.serverUpdatesSent > state.serverUpdatesEnded;
        const wasmBusy = state.wasmUpdatesSent > state.wasmUpdatesEnded;
        const serverUnattached = state.serverUpdatesEnded === 0 && state.serverRootsAttached < serverMarkers;
        if (serverBusy || serverUnattached || state.lastBatchBeforeAttach !== null) {
            pending.push('server');
        }
        if (wasmBusy || (markerTypes.has('webassembly') && state.wasmUpdatesEnded === 0)) {
            pending.push('webassembly');
        }
        if (markerTypes.has('auto') && state.serverUpdatesEnded === 0 && state.wasmUpdatesEnded === 0) {
            pending.push('auto');
        }
        return pending;
    }

    function snapshot(status, startedAt) {
        const now = performance.now();
        const busy = busyCalls();
        return {
            status,
            dotNetCalls: countByName(busy),
            parkedCalls: countByName([...state.dotNetCalls].filter(call => !busy.includes(call))),
            pendingBatches: Math.max(0, state.lastBatchReceived - state.lastBatchCompleted),
            lastBatchReceived: state.lastBatchReceived,
            lastBatchCompleted: state.lastBatchCompleted,
            pendingRootComponents: pendingRootComponents(),
            navigating: state.navigating,
            msSinceActivity: now - state.lastActivity,
            elapsedMs: startedAt === undefined ? 0 : now - startedAt,
            failure: state.failure,
            decodeErrors: state.decodeErrors,
            rootUpdates: {
                serverSent: state.serverUpdatesSent,
                serverEnded: state.serverUpdatesEnded,
                serverAttached: state.serverRootsAttached,
                serverMarkers,
                awaitingAttachRender: state.lastBatchBeforeAttach === null ? 0 : 1,
                wasmSent: state.wasmUpdatesSent,
                wasmEnded: state.wasmUpdatesEnded,
            },
            markerTypes: [...markerTypes],
        };
    }

    function isBusy() {
        return busyCalls().length > 0
            || state.lastBatchReceived > state.lastBatchCompleted
            || pendingRootComponents().length > 0
            || state.navigating;
    }

    // Resolves once nothing is busy and nothing has changed for quietMs, or on failure or timeout.
    function waitUntil(isReady, quietMs, timeoutMs) {
        const startedAt = performance.now();
        return new Promise(resolve => {
            let timer;
            const finish = status => {
                clearTimeout(timer);
                waiters.delete(check);
                resolve(snapshot(status, startedAt));
            };
            const check = () => {
                clearTimeout(timer);
                const now = performance.now();
                if (state.failure) {
                    return finish('failed');
                }
                const timeLeft = timeoutMs - (now - startedAt);
                if (timeLeft <= 0) {
                    return finish('timeout');
                }
                if (!isReady()) {
                    timer = setTimeout(check, timeLeft);
                    return;
                }
                const quietLeft = quietMs - (now - state.lastActivity);
                if (quietLeft <= 0) {
                    return finish('ready');
                }
                timer = setTimeout(check, Math.min(quietLeft, timeLeft));
            };
            waiters.add(check);
            check();
        });
    }

    window.__blazorTest = {
        snapshot: () => snapshot('snapshot'),
        idle: (quietMs, timeoutMs) => waitUntil(() => !isBusy(), quietMs, timeoutMs),
        interactive: timeoutMs => waitUntil(() => !state.navigating && pendingRootComponents().length === 0
            && state.lastBatchReceived === state.lastBatchCompleted, 0, timeoutMs),
        hasHandlers: element => Object.keys(element).some(key => key.startsWith('_blazorEvents_')),
    };
})();
