using Microsoft.Playwright;

namespace Spike.Tests;

// One app and one Playwright per test class, so each class reads only its own server log.
public class SpikeHost : IAsyncLifetime
{
    private readonly Dictionary<string, IBrowser> _browsers = [];
    private readonly string _tracker = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tracker.js"));
    private IPlaywright? _playwright;

    public SpikeApp App { get; private set; } = null!;

    protected virtual Dictionary<string, string> AppSettings => [];

    public async ValueTask InitializeAsync()
    {
        App = new SpikeApp(AppSettings);
        _playwright = await Playwright.CreateAsync();
    }

    public async Task<IPage> NewPageAsync(string browserName, bool withTracker = true)
    {
        if (!_browsers.TryGetValue(browserName, out var browser))
        {
            browser = await _playwright![browserName].LaunchAsync();
            _browsers[browserName] = browser;
        }

        var context = await browser.NewContextAsync(new() { BaseURL = App.BaseUrl });
        if (withTracker)
        {
            await context.AddInitScriptAsync(_tracker);
        }

        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var browser in _browsers.Values)
        {
            await browser.CloseAsync();
        }

        _playwright?.Dispose();
        await App.DisposeAsync();
    }
}

// A server that takes 300 ms longer to start a circuit, so a click straight after GotoAsync can beat the handlers.
public class SlowCircuitHost : SpikeHost
{
    protected override Dictionary<string, string> AppSettings => new() { ["NegotiateDelayMs"] = "300" };
}
