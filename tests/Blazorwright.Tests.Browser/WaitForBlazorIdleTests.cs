using Microsoft.Playwright;

namespace Blazorwright.Tests.Browser;

public class WaitForBlazorIdleTests(Browsers browsers, TestAppFixture app) : IClassFixture<TestAppFixture>
{
    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "serverjs")]
    [InlineData("firefox", "serverjs")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorIdleAsync_ShouldReturnAfterAwaitedWork_WhenHandlerAwaitsDelay(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/delayed");

        await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        await page.WaitForBlazorIdleAsync();

        Assert.Equal("Loaded", await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server", "Run", "Done by service")]
    [InlineData("firefox", "server", "Run", "Done by service")]
    [InlineData("chromium", "serverjs", "Burst", "Burst done")]
    [InlineData("firefox", "serverjs", "Burst", "Burst done")]
    public async Task WaitForBlazorIdleAsync_ShouldReturnAfterLastRender_WhenServerRendersRepeatedly(string browser, string mode,
        string button, string finalStatus)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/updates");

        await page.GetByRole(AriaRole.Button, new() { Name = button }).ClickAsync();
        await page.WaitForBlazorIdleAsync();

        Assert.Equal(finalStatus, await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorIdleAsync_ShouldReturnWhileDialogIsOpen_WhenHandlerAwaitsDialogResult(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/rename");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
        await page.WaitForBlazorIdleAsync();

        await page.GetByLabel("Name", new() { Exact = true }).FillAsync("Ada");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await page.WaitForBlazorIdleAsync();

        Assert.Equal("Saved Ada", await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorIdleAsync_ShouldWaitForJSCall_WhenFireAndForgetWorkAwaitsIt(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/measure");

        await page.GetByRole(AriaRole.Button, new() { Name = "Show tip" }).ClickAsync();
        await page.WaitForBlazorIdleAsync();

        Assert.Equal("Width 240", await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorIdleAsync_ShouldStartAgainOnNewPage_WhenPageReloadsDuringWait(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/delayed");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        var idle = page.WaitForBlazorIdleAsync();

        await page.ReloadAsync();
        await idle;

        Assert.Equal("Idle", await page.GetByRole(AriaRole.Status).TextContentAsync());
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorIdleAsync_ShouldThrowNamingTheCall_WhenHandlerNeverFinishes(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app, new() { Timeout = TimeSpan.FromSeconds(1) });
        await page.GotoBlazorAsync($"/{mode}/stuck");
        await page.GetByRole(AriaRole.Button, new() { Name = "Wait forever" }).ClickAsync();

        var exception = await Assert.ThrowsAsync<BlazorTimeoutException>(() => page.WaitForBlazorIdleAsync());

        Assert.StartsWith("Not idle after 1 s: 1 call in flight (DispatchEventAsync), 0 render batches pending", exception.Message);
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task WaitForBlazorIdleAsync_ShouldThrowCircuitException_WhenHandlerThrows(string browser, string mode)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync($"/{mode}/throws");
        await page.GetByRole(AriaRole.Button, new() { Name = "Throw" }).ClickAsync();

        var exception = await Assert.ThrowsAsync<BlazorCircuitException>(() => page.WaitForBlazorIdleAsync());

        Assert.Equal(CircuitFailure.UnhandledException, exception.Failure);
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task WaitForBlazorIdleAsync_ShouldIncludeServerException_WhenServerHandlerThrows(string browser)
    {
        var page = await browsers.NewTrackedPageAsync(browser, app);
        await page.GotoBlazorAsync("/server/throws");
        await page.GetByRole(AriaRole.Button, new() { Name = "Throw" }).ClickAsync();

        var exception = await Assert.ThrowsAsync<BlazorCircuitException>(() => page.WaitForBlazorIdleAsync());

        Assert.Contains("Server warnings and errors since this test's browser context opened", exception.Message);
        Assert.Contains("System.InvalidOperationException: The throw button was clicked.", exception.Message);
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task WaitForBlazorIdleAsync_ShouldThrowNotSupported_WhenTrackingIsNotInstalled()
    {
        var page = await browsers.NewUntrackedPageAsync("chromium", app);
        await page.GotoAsync("/server/counter");

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => page.WaitForBlazorIdleAsync());

        Assert.Contains("AddBlazorTrackingAsync", exception.Message);
        await page.Context.CloseAsync();
    }
}
