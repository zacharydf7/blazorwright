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

    public async Task<IPage> NewTrackedPageAsync(string browserName, string baseUrl, BlazorTestOptions? options = null)
    {
        var context = await NewContextAsync(browserName, baseUrl);
        await context.AddBlazorTrackingAsync(options);
        return await context.NewPageAsync();
    }

    public async Task<IPage> NewUntrackedPageAsync(string browserName, string baseUrl)
    {
        var context = await NewContextAsync(browserName, baseUrl);
        return await context.NewPageAsync();
    }

    private async Task<IBrowserContext> NewContextAsync(string browserName, string baseUrl)
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

        return await (await browser).NewContextAsync(new() { BaseURL = baseUrl });
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
