# Other test frameworks

The waits are extension methods on Playwright's `IPage`, `ILocator` and `IBrowserContext` in the `Blazorwright`
package, so they work with any test framework. Only `BlazorPageTest` is specific to xUnit v3.

Without the base class, a test needs to do two things itself:

1. Install the tracker on the browser context before its first page opens, with `AddBlazorTrackingAsync`.
2. Point the context at the app, with `BaseURL`.

## Plain Playwright

```csharp
using Blazorwright;
using Microsoft.Playwright;

await using var app = new AppFixture();
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync();
await using var context = await browser.NewContextAsync(new() { BaseURL = app.BaseUrl });
await context.AddBlazorTrackingAsync(serverLog: app.Log);

var page = await context.NewPageAsync();
await page.GotoBlazorAsync("/counter");
```

`AddBlazorTrackingAsync` takes the options that become the default for that context's waits, and the app's server log,
which adds the server's warnings and errors to failure messages. Both are optional.

## NUnit

With `Microsoft.Playwright.NUnit`, derive from `ContextTest` and open the page in a setup method. NUnit runs the base
class's setup first, so the context exists by then:

```csharp
using Blazorwright;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

public class CounterTests : ContextTest
{
    private IPage _page = null!;

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = App.Instance.BaseUrl };

    [SetUp]
    public async Task OpenPageAsync()
    {
        await Context.AddBlazorTrackingAsync(serverLog: App.Instance.Log);
        _page = await Context.NewPageAsync();
    }

    [Test]
    public async Task Counter_ShouldIncrement_WhenClicked()
    {
        await _page.GotoBlazorAsync("/counter");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        await _page.WaitForBlazorIdleAsync();

        await Expect(_page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Current count: 1");
    }
}

[SetUpFixture]
public class App
{
    public static AppFixture Instance { get; } = new();

    [OneTimeTearDown]
    public async Task StopAsync() => await Instance.DisposeAsync();
}
```

MSTest works the same way with `Microsoft.Playwright.MSTest` and `[TestInitialize]`. Base classes for NUnit and MSTest
that also keep traces and videos are planned; see
[issue #15](https://github.com/zacharydf7/blazorwright/issues/15).
