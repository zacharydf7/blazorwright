# Failure messages and artifacts

A Blazorwright wait fails in one of two ways, and each says why.

## Timeouts

When a wait runs out of time, it throws `BlazorTimeoutException` (a `TimeoutException`) and lists what was still
running:

```
Blazorwright.BlazorTimeoutException: Not idle after 30 s: 1 call in flight (DispatchEventAsync), 0 render batches
pending, last activity 29950 ms ago.
```

Read it from left to right:

| Part | Meaning |
| --- | --- |
| `1 call in flight (DispatchEventAsync)` | A .NET call hasn't returned. See the names below. |
| `1 call waiting on a dialog (...)` | A call that opened a dialog; it doesn't count while the dialog is open. See [Dialogs](dialogs.md). |
| `2 render batches pending` | The server sent renders the browser hasn't applied. |
| `root components still attaching (...)` | Interactive components that haven't been attached yet. |
| `an enhanced navigation in progress` | A navigation that hasn't finished. |
| `last activity 29950 ms ago` | The last time anything changed. A small number means the page kept changing. |

Calls are named by how they reach .NET: every event handler as `DispatchEventAsync`, a `[JSInvokable]` instance method
by its name, a static one as `Assembly.Method`, and a call from .NET to JavaScript as `JS <identifier>`.

A wait for a locator says the element was missing or had no Blazor event handlers. If the page was still loading when
the wait started and never finished, the message says so.

## The app failed

When the app fails during a wait, the wait stops at once and throws `BlazorCircuitException` instead of timing out. Its
`Failure` property says which kind of failure it was:

| `CircuitFailure` | Raised when |
| --- | --- |
| `UnhandledException` | The server reported an unhandled exception on the circuit, or Blazor showed its error UI (`#blazor-error-ui`) |
| `Disconnected` | The circuit's WebSocket closed unexpectedly, or reconnecting failed |
| `None` | Blazorwright couldn't read a circuit message, so its counts can't be trusted |

[`FailOn`](options.md) chooses which app failures stop a wait. An unreadable circuit message (`None`) always stops it,
whatever `FailOn` says.

## The server's log

When the app runs in the test process, every failure message ends with the warnings and errors the server logged since
the test's browser context opened, including exception stack traces:

```
Server warnings and errors since this test's browser context opened (tests sharing the app add to them too):
Error Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost: Unhandled exception in circuit '...'.
System.InvalidOperationException: The report has no rows.
   at ReportPage.LoadReportAsync() in ReportPage.razor:line 41
```

Tests that run at the same time and share the app add their lines too, so a line may belong to another test.

## Traces and videos

`BlazorPageTest` records a Playwright trace and a video of every test, and keeps them only when the test fails:

```
bin/Debug/net10.0/TestResults/Blazor/<test name>/trace.zip
bin/Debug/net10.0/TestResults/Blazor/<test name>/video-1.webm
```

The trace has a screenshot and a DOM snapshot for every action, plus the console and network logs. Open it with:

```
pwsh bin/Debug/net10.0/playwright.ps1 show-trace bin/Debug/net10.0/TestResults/Blazor/<test name>/trace.zip
```

or drop it on [trace.playwright.dev](https://trace.playwright.dev). Override `Artifacts` to keep less:

```csharp
protected override FailureArtifacts Artifacts => FailureArtifacts.Trace;
```

### Keeping them in CI

Upload the folder when a run fails. With GitHub Actions:

```yaml
- uses: actions/upload-artifact@v4
  if: failure()
  with:
    name: blazor-failures
    path: '**/TestResults/Blazor/'
```
