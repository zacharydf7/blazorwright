# How it works

Blazorwright adds a script to every page the test opens, with Playwright's `AddInitScriptAsync`, so it runs before any
of the page's own scripts. The script hooks into Blazor's public JavaScript surface and the circuit's WebSocket, and
keeps a few counters. The waits ask the page for those counters.

Nothing is added to the app under test, and nothing changes how the app behaves.

## What the script counts

**.NET calls.** Every DOM event a component handles, and every `[JSInvokable]` call, goes through
`DotNet.invokeMethodAsync` or `invokeMethodAsync` on a `DotNetObjectReference`. The script wraps both and counts a call
from the moment it starts until its promise settles. On Blazor Server, the promise settles when the server has run the
handler to completion, including everything it awaits.

**Calls from .NET to JavaScript.** When .NET calls `IJSRuntime.InvokeAsync`, the call arrives through the dispatcher
Blazor attaches to `DotNet`. The script counts it until the JavaScript function has settled and its result is on its way
back. A JavaScript function that never settles keeps idle busy, and the timeout message names it.

**Render batches.** On Blazor Server, renders arrive over the circuit's WebSocket as SignalR messages in MessagePack
format. The script decodes the framing, counts each `JS.RenderBatch` message, and counts it done when the browser sends
`OnRenderCompleted` for it. On WebAssembly, .NET runs in the page and applies its renders to the DOM itself, so no
batches are in transit.

**Root components.** Interactive components arrive as markers in the HTML. The script records each marker as the parser
inserts it, and counts the component as pending until Blazor has attached it to the circuit or the WebAssembly runtime.

**Enhanced navigation.** Blazor raises `enhancednavigationstart` and `enhancednavigationend`. The script counts a
navigation as in progress between them, plus one task after the end, because Blazor adds the new page's components in
a microtask after it.

**The DOM.** A `MutationObserver` records the time of the last change. Idle needs the page to stay unchanged for the
quiet period, which covers short work that nothing counts.

## Failures

The script also watches for the app failing, so a wait can stop at once:

- a circuit message that reports an unhandled error;
- Blazor's error UI (`#blazor-error-ui`) being shown;
- the circuit's WebSocket closing without a clean close handshake;
- the reconnect UI reporting that reconnecting failed or was rejected.

If the script ever can't decode a circuit message, its batch count can no longer be trusted, so every wait fails with
a message that says so rather than returning early.

## The waits

Each page wait runs one call into the page that resolves when its condition holds or the timeout passes. The page
checks the condition on every change it sees and again when the quiet period ends, so a wait returns as soon as the app
is ready. When the page navigates during a wait, the wait starts again on the new page with the time it has left.

The locator wait instead checks the element every 20 ms, finding it again each time.

## Dependence on Blazor internals

The hooks above use parts of Blazor's JavaScript that are public but not documented as a contract: the `DotNet` object,
the circuit's message names and the root component markers. Each Blazorwright release is tested against the Blazor
version it supports, and a change in those parts shows up as a failing contract test before a release.
