# Troubleshooting

Start with the failure message: it says what was still running. [Failure messages and artifacts](failures.md) explains
each part.

## Idle times out with a call in flight

```
Not idle after 30 s: 1 call in flight (DispatchEventAsync), 0 render batches pending, last activity 29950 ms ago.
```

An event handler hasn't returned. Either it's slower than the timeout, or it's waiting for something that never comes.

- If the work is just slow, such as a report or a file upload, raise the [timeout](options.md) for that test class.
- If it awaits something from the user, such as a dialog that Blazorwright doesn't recognize, give the dialog
  `aria-modal="true"`. See [Dialogs](dialogs.md#popovers-and-unmarked-dialogs).
- Check the server log lines at the end of the message for an exception the handler swallowed and retried.

## Idle times out with a JavaScript call in flight

```
Not idle after 30 s: 1 call in flight (JS chartInterop.render), 0 render batches pending, last activity 29910 ms ago.
```

A call from .NET to JavaScript hasn't settled. The function, or a promise it returned, never resolved or rejected.
Look at that function in the trace's console log.

## Idle times out with nothing busy

```
Not idle after 30 s: no calls in flight, 0 render batches pending, last activity 12 ms ago.
```

Nothing is running, but the page changed 12 ms ago, so it never stayed quiet for the quiet period. Something changes
the DOM all the time: a clock, a countdown, a progress animation written in JavaScript, or a library that rewrites a
style on a timer. Open the trace and look at which element keeps changing between snapshots. Then either stop it in
the test environment, or use `WaitForBlazorInteractiveAsync` and Playwright assertions on that page.

## Idle times out with render batches pending

```
Not idle after 30 s: no calls in flight, 2 render batches pending, last activity 29990 ms ago.
```

The server sent renders that the browser never applied. That usually means the browser threw while applying them; the
trace's console log shows the error. A JavaScript error in a component's script can also stop rendering.

## "Blazor tracking isn't installed on this page"

The wait throws `NotSupportedException` when the page has no tracker. The tracker has to be installed on the browser
context before the page opens. `BlazorPageTest` does this for its `Page`. A page from another context, or one opened
before `AddBlazorTrackingAsync` was called, has no tracker.

## A click does nothing

The page probably wasn't interactive yet. Open pages with `GotoBlazorAsync` rather than `GotoAsync`, or wait for the
element with `locator.WaitForBlazorInteractiveAsync()` before clicking. See [Prerendered pages](prerendering.md).

## An assertion sees the old page

A wait returned before the change it was meant to cover. Possible causes:

- The test didn't wait after the action. Add `WaitForBlazorIdleAsync()` after it.
- The change comes from work nothing awaits, such as `_ = LoadAsync();`. Wait for the result with a Playwright
  assertion; see [the waits](waits.md#waiting-for-something-specific).

## The wait fails with `BlazorCircuitException` straight away

The app failed: the server threw, Blazor showed its error UI, or the circuit disconnected. The message includes the
server's exception when the app runs in the test process. If the test is checking the app's own error handling, leave
that failure out of [`FailOn`](options.md#letting-a-failure-through).
