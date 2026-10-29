# Blazorwright

Deterministic Playwright tests for Blazor apps on .NET 10.

`await Page.WaitForBlazorIdleAsync()` tells a test that the app has finished reacting to what the test just did: no
event handler still running, no server render still on its way to the browser. It needs no changes to the app under
test, so retries and fixed sleeps can go.

Status: v0.1 is in development and not on NuGet yet.

## Why tests of Blazor apps are flaky

Playwright waits for an element to be visible and enabled, but it can't see what Blazor is doing:

- A prerendered button is visible before Blazor attaches its click handler, so an early click does nothing.
- After a click, the handler may still be awaiting work on the server, and the render it produces may still be on its
  way to the browser. An assertion that runs now sees the old page.
- A test that checks something did *not* happen has nothing to wait for, so it sleeps.

Blazorwright watches Blazor from inside the page: the .NET calls in flight, the render batches the server has sent and
the browser hasn't applied, the root components still attaching, and the DOM. A wait returns when all of them are done.

## Getting started

Add the xUnit v3 package to your test project and install the Playwright browsers once:

```
dotnet add package Blazorwright.Xunit
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

Make the app's `Program` class visible to the tests with `public partial class Program;` at the end of `Program.cs`.
Then add one fixture for the test assembly and derive test classes from `BlazorPageTest`:

```csharp
using Blazorwright;
using Blazorwright.Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(AppFixture))]

// The app runs in-process on real Kestrel, on a port the OS picks.
public class AppFixture : BlazorAppFixture<Program>
{
    protected override void ConfigureApp(IWebHostBuilder app) => app.UseSetting("Database:Provider", "InMemory");
}

public class CounterTests(AppFixture app) : BlazorPageTest(app)
{
    [Fact]
    public async Task Counter_ShouldIncrement_WhenClicked()
    {
        await Page.GotoBlazorAsync("/counter");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        await Page.WaitForBlazorIdleAsync();

        Assert.Equal("Current count: 1", await Page.GetByRole(AriaRole.Status).TextContentAsync());
    }
}
```

## The waits

| Method | Returns when |
| --- | --- |
| `Page.GotoBlazorAsync(path)` | The page has loaded, its interactive components are attached, and the app is idle |
| `Page.WaitForBlazorInteractiveAsync()` | Every interactive root component on the page has its event handlers attached |
| `Page.WaitForBlazorIdleAsync()` | No .NET calls are in flight, no render batches are pending, and nothing has changed for the quiet period |
| `locator.WaitForBlazorInteractiveAsync()` | That element exists and has its event handlers attached |

Idle also waits for calls from .NET to JavaScript and for enhanced navigation. A handler that opens a dialog and awaits
its result (`await dialog.Result`) doesn't block idle while the dialog is open; it counts again once the dialog closes.
A dialog is any visible element with `aria-modal="true"`, `role="dialog"` or `role="alertdialog"`, or a modal `<dialog>`.

## Options

Override `Options` in a test class, or pass options to a single wait. A wait times out after 30 s by default, like
Playwright's own; raise `Timeout` when the app has handlers that run longer, such as file processing on the server.

```csharp
protected override BlazorTestOptions Options => new()
{
    Timeout = TimeSpan.FromSeconds(60),
    QuietPeriod = TimeSpan.FromMilliseconds(100),
    FailOn = CircuitFailure.UnhandledException | CircuitFailure.Disconnected,
};

protected override FailureArtifacts Artifacts => FailureArtifacts.Trace | FailureArtifacts.Video;
```

## When a wait fails

A wait that runs out of time says what was still running, and adds the warnings and errors the server logged since the
test's browser context opened. Tests that share the app and run at the same time add to those lines too.

```
Blazorwright.BlazorTimeoutException: Not idle after 60 s: 1 call in flight (DispatchEventAsync), 0 render batches
pending, last activity 59950 ms ago.
```

If the app fails during a wait (an unhandled exception, Blazor's error UI, or a lost circuit), the wait stops at once
with `BlazorCircuitException` instead of timing out. When the app runs in the test process, the server's exception is
in the message.

A failed test keeps a Playwright trace and a video under `TestResults/Blazor/<test>/`; passing tests keep nothing. Open
a trace with `pwsh bin/Debug/net10.0/playwright.ps1 show-trace <path>`.

## Without the base class

The waits are extension methods on Playwright's `IPage` and `ILocator`, so they work with any test framework. Install
the tracker on a browser context before its first page opens:

```csharp
await context.AddBlazorTrackingAsync(options, app.Log);
```

`app.Log` is optional; it adds the server's log lines to failure messages when the app runs in the test process.

## Limits

- **Work nothing awaits.** A handler that starts work and returns (`_ = LoadAsync();`) leaves nothing to count. The
  quiet period (100 ms by default) covers short work like that; for longer work, wait for its result with Playwright's
  `Expect`.
- **A JavaScript call that never settles** keeps idle busy until it times out, and the message names the call.
- **A non-modal popover with `role="dialog"`** stops its handler from counting while it is open, like a dialog does.
- **Blazor Server needs WebSockets**, the default transport.
- **Checks for passing states.** After a wait, a short-lived state such as a loading message may already be gone, so a
  test that expects to see it can time out. Check the state the app ends in instead.

## Support

.NET 10 with xUnit v3. Interactive Server (with `blazor.web.js` or the older `blazor.server.js`), Interactive
WebAssembly and Interactive Auto, prerendered or not, and static pages with enhanced navigation. Tested on Chromium and
Firefox; WebKit should work but isn't tested.

The tracker relies on Blazor internals, so CI runs the browser tests against the Blazor version each release supports.

## License

MIT
