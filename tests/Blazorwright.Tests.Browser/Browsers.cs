using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(Blazorwright.Tests.Browser.Browsers))]

namespace Blazorwright.Tests.Browser;

// One Playwright for the test run, launching each browser the first time a test asks for it.
public class Browsers : IAsyncLifetime
{
    private readonly Dictionary<string, Task<IBrowser>> _browsers = [];
    private readonly Lock _lock = new();
    private IPlaywright _playwright = null!;

    public async ValueTask InitializeAsync() => _playwright = await Playwright.CreateAsync();

    public async Task<IPage> NewTrackedPageAsync(string browserName, TestAppFixture app, BlazorTestOptions? options = null)
    {
        var context = await NewContextAsync(browserName, new() { BaseURL = app.BaseUrl });
        await context.AddBlazorTrackingAsync(options, app.Log);
        return await context.NewPageAsync();
    }

    public async Task<IPage> NewUntrackedPageAsync(string browserName, TestAppFixture app)
    {
        var context = await NewContextAsync(browserName, new() { BaseURL = app.BaseUrl });
        return await context.NewPageAsync();
    }

    public async Task<IBrowserContext> NewContextAsync(string browserName, BrowserNewContextOptions options)
    {
        Task<IBrowser> browser;
        lock (_lock)
        {
            if (!_browsers.TryGetValue(browserName, out browser!))
            {
                browser = _playwright[browserName].LaunchAsync();
                _browsers[browserName] = browser;
            }
        }

        return await (await browser).NewContextAsync(options);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var browser in _browsers.Values)
        {
            await (await browser).CloseAsync();
        }

        _playwright.Dispose();
    }
}
