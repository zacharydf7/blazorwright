# Support and limits

## What's supported

| | Supported |
| --- | --- |
| .NET | .NET 10 |
| Test frameworks | xUnit v3 with `BlazorPageTest`; any framework with the extension methods |
| Render modes | Interactive Server, Interactive WebAssembly, Interactive Auto, prerendered or not |
| Static pages | Static server rendering with enhanced navigation |
| Scripts | `blazor.web.js`, and the older `blazor.server.js` |
| Browsers | Chromium and Firefox, tested in CI; WebKit should work but isn't tested yet |
| Operating systems | Windows and Linux, tested in CI |

The tracker relies on parts of Blazor's JavaScript that aren't a documented contract, so each release is tested
against the Blazor version it supports. See [How it works](how-it-works.md#dependence-on-blazor-internals).

## Limits

**Work nothing awaits.** A handler that starts work and returns (`_ = LoadAsync();`) leaves nothing to count. The
quiet period, 100 ms by default, covers short work like that. For longer work, wait for its result with a Playwright
assertion such as `Expect(locator).ToHaveTextAsync(...)`.

**A JavaScript call that never settles.** A call from .NET to JavaScript counts until its promise settles. One that
never settles keeps idle busy until the wait times out, and the message names the call.

**Modal dialogs marked only with `role="dialog"`.** Such a dialog doesn't stop the handler that awaits it from
counting, so idle times out while it's open. See
[Dialogs](dialogs.md#popovers-and-unmarked-dialogs).

**WebSockets only.** Blazor Server must use WebSockets, its default transport. If a proxy or a setting forces
long polling or Server-Sent Events, render batches can't be counted.

**Short-lived states.** After a wait, a state such as a loading message may already be gone, so a test that
expects to see it can time out. Check the state the app ends in instead.

**Pages that never stop changing.** A clock, an elapsed-time label or an animation driven by the DOM restarts the quiet
period on every change, so idle never returns on that page. Use `WaitForBlazorInteractiveAsync` there, and Playwright
assertions for results.
