using Microsoft.Playwright;

namespace Blazorwright.Tests.Browser;

public class WaitForBlazorInteractiveTests(Browsers browsers, SlowCircuitAppFixture app) : IClassFixture<SlowCircuitAppFixture>
{
    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "serverjs")]
    [InlineData("firefox", "serverjs")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    [InlineData("chromium", "auto")]
    [InlineData("firefox", "auto")]
    public async Task WaitForBlazorInteractiveAsync_ShouldAttachHandlers_WhenPageIsPrerendered(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await page.GotoAsync($"/{mode}/counter");

        await page.WaitForBlazorInteractiveAsync();

        Assert.True(await HasHandlersAsync(button));
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    [InlineData("chromium", "auto")]
    [InlineData("firefox", "auto")]
    public async Task WaitForBlazorInteractiveAsync_ShouldWaitForNewComponents_WhenEnhancedNavigationAddsThem(string browser,
        string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/delayed");
        await page.GetByRole(AriaRole.Link, new() { Name = "Counter page" }).ClickAsync();

        await page.WaitForBlazorInteractiveAsync();

        var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        Assert.True(await button.CountAsync() == 1 && await HasHandlersAsync(button));
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorInteractiveAsync_ShouldWaitForElementHandlers_WhenCalledOnLocator(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await page.GotoAsync($"/{mode}/counter");

        await button.WaitForBlazorInteractiveAsync();

        await button.ClickAsync();
        await page.WaitForBlazorIdleAsync();
        Assert.Equal("Current count: 1", await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorInteractiveAsync_ShouldThrowCircuitException_WhenAppFailsDuringLocatorWait(string browser,
        string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/throws");
        await page.GetByRole(AriaRole.Button, new() { Name = "Throw" }).ClickAsync();

        var exception = await Assert.ThrowsAsync<BlazorCircuitException>(
            () => page.GetByRole(AriaRole.Button, new() { Name = "Never shown" }).WaitForBlazorInteractiveAsync());

        Assert.Equal(CircuitFailure.UnhandledException, exception.Failure);
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task WaitForBlazorInteractiveAsync_ShouldThrowBlazorTimeout_WhenLocatorNeverMatches()
    {
        var page = await browsers.NewTrackedPageAsync("chromium", app, new() { Timeout = TimeSpan.FromSeconds(1) });
        await page.GotoBlazorAsync("/server/counter");

        var exception = await Assert.ThrowsAsync<BlazorTimeoutException>(
            () => page.GetByRole(AriaRole.Button, new() { Name = "Never shown" }).WaitForBlazorInteractiveAsync());

        Assert.Contains("was not interactive after 1 s: it was missing or had no Blazor event handlers.", exception.Message);
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task GotoBlazorAsync_ShouldReturnReadyPage_WhenPageIsPrerendered(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/counter");

        await page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        await page.WaitForBlazorIdleAsync();

        Assert.Equal("Current count: 1", await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    private static Task<bool> HasHandlersAsync(ILocator element) =>
        element.EvaluateAsync<bool>("element => window.__blazorTest.hasHandlers(element)");
}
