# Getting started

This tutorial adds a Playwright test project to a Blazor app and writes a first test. It takes about ten minutes.

You need the .NET 10 SDK and a Blazor Web App on .NET 10. Any render mode works: Interactive Server, WebAssembly or
Auto.

## 1. Create the test project

Create an xUnit v3 project next to the app and reference the app from it:

```
dotnet new install xunit.v3.templates
dotnet new xunit3 -o MyApp.Tests
dotnet add MyApp.Tests reference MyApp
dotnet add MyApp.Tests package Blazorwright.Xunit
```

`Blazorwright.Xunit` brings in Blazorwright itself, Playwright and `Microsoft.AspNetCore.Mvc.Testing`.

## 2. Install a browser

Playwright downloads its browsers once per machine. Build the test project, then run the script the build produced:

```
dotnet build MyApp.Tests
pwsh MyApp.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

On Linux CI agents, add `--with-deps` to install the browser's system libraries too.

## 3. Make the app's `Program` class visible

The fixture starts the app from its `Program` class, which is internal by default. Add this line at the end of the
app's `Program.cs`:

```csharp
public partial class Program;
```

## 4. Add a fixture for the app

One fixture runs the app for the whole test assembly. Add `AppFixture.cs` to the test project:

```csharp
using Blazorwright;

[assembly: AssemblyFixture(typeof(AppFixture))]

// The app runs in-process on real Kestrel, on a port the OS picks.
public class AppFixture : BlazorAppFixture<Program>;
```

The app starts the first time a test needs it and stops when the test run ends. [Hosting the app](hosting.md) shows
how to change its settings and services for the tests.

## 5. Write a test

Derive the test class from `BlazorPageTest`. Each test gets a fresh browser context and a `Page` that has Blazor tracking
installed and opens paths relative to the app:

```csharp
using Blazorwright;
using Blazorwright.Xunit;
using Microsoft.Playwright;

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

Three things happen here:

1. `GotoBlazorAsync` opens the page and waits until its interactive components have their handlers attached and the
   app is idle. The click can't land before the button works.
2. `WaitForBlazorIdleAsync` waits until the click's handler has finished and its render has reached the browser.
3. The assertion reads the page once. It needs no retry, because the page is already in its final state.

## 6. Run it

```
dotnet test MyApp.Tests
```

The test runs headless in Chromium. To watch it, set `HEADED=1` before running. To run in Firefox or WebKit, set
`BROWSER=firefox` or `BROWSER=webkit` after installing that browser.

If the test fails, its trace and video are under `bin/Debug/net10.0/TestResults/Blazor/<test name>/`. Open the trace
with:

```
pwsh MyApp.Tests/bin/Debug/net10.0/playwright.ps1 show-trace <path to trace.zip>
```

## Next steps

- [The waits](waits.md): which wait to use when.
- [From sleeps to waits](replacing-sleeps.md): move an existing Playwright suite over.
- [Options](options.md): timeouts, the quiet period, and which failures stop a wait.
