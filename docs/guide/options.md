# Options

`BlazorTestOptions` controls how long the waits may take, what counts as quiet, and which failures stop them early.

| Setting | Default | Meaning |
| --- | --- | --- |
| `Timeout` | 30 s | How long a wait may take before it throws `BlazorTimeoutException`. |
| `QuietPeriod` | 100 ms | How long the page must go without DOM changes or Blazor activity before the app counts as idle. It covers work no handler awaits. |
| `WaitForAriaBusy` | `false` | Whether idle also waits for visible elements marked `aria-busy="true"`. |
| `FailOn` | `UnhandledException \| Disconnected` | Failures that end a wait with `BlazorCircuitException` instead of letting it time out. |

The timeout matches Playwright's default, but it's separate: `SetDefaultTimeout` doesn't change it.

## For a test class

Override `Options` in a class that derives from `BlazorPageTest`:

```csharp
public class ReportTests(AppFixture app) : BlazorPageTest(app)
{
    // Report generation runs in one server call that can take a minute.
    protected override BlazorTestOptions Options => new() { Timeout = TimeSpan.FromMinutes(2) };
}
```

To share settings between classes, put the override in a base class of your own:

```csharp
public abstract class AppPageTest(AppFixture app) : BlazorPageTest(app)
{
    protected override BlazorTestOptions Options => new() { Timeout = TimeSpan.FromSeconds(60) };
}
```

## For one wait

Pass options to the wait. They replace the class's options for that wait, so start from the class's to keep its other
settings:

```csharp
await Page.WaitForBlazorIdleAsync(Options with { QuietPeriod = TimeSpan.FromMilliseconds(500) });
```

## Without the base class

Options passed to `AddBlazorTrackingAsync` become the default for every wait on that browser context:

```csharp
await context.AddBlazorTrackingAsync(new BlazorTestOptions { Timeout = TimeSpan.FromSeconds(60) });
```

## Choosing a quiet period

The quiet period covers work nothing awaits, such as a component that loads data from a timer or a
`_ = LoadAsync();` call. A longer quiet period covers more of it, but every idle wait takes at least that long.
Keep the default, and when one result takes longer, wait for it with a Playwright assertion instead.

## Waiting for aria-busy

Many components mark work in progress with `aria-busy="true"` for screen readers: a grid while its rows load, a button
while its action runs, a skeleton while its content is missing. With `WaitForAriaBusy` on, idle counts every visible
element marked that way as busy, so it also waits for loads that no handler awaits:

```csharp
protected override BlazorTestOptions Options => new() { WaitForAriaBusy = true };
```

It's off by default. A spinner or skeleton that stays busy on purpose keeps idle busy until the wait times out, and the
message names it:

```
Not idle after 30 s: no calls in flight, 0 render batches pending, 1 element marked aria-busy (div.skeleton), last
activity 29870 ms ago.
```

## Letting a failure through

To test the app's own error handling, such as a reconnect dialog, leave that failure out of `FailOn`:

```csharp
protected override BlazorTestOptions Options => new() { FailOn = CircuitFailure.UnhandledException };
```

Use `CircuitFailure.None` to let every app failure through. A wait then times out instead, if the app never becomes idle.

## Failure artifacts

`BlazorPageTest.Artifacts` chooses what a failed test keeps; see
[Failure messages and artifacts](failures.md#traces-and-videos).

| `FailureArtifacts` | Keeps |
| --- | --- |
| `Trace` | A Playwright trace with screenshots, DOM snapshots and sources |
| `Video` | A video of each page |
| `None` | Nothing, and records nothing |
