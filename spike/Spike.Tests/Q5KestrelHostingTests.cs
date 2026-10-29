using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q5: the app starts in-process on UseKestrel(0) and serves the class library's assets and the WebAssembly runtime.
public class Q5KestrelHostingTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task StartServer_ShouldServeAllStaticAssets_WhenAppRunsOnKestrel(string browser)
    {
        var results = new RunResults($"Q5-{browser}");

        for (var run = 0; run < RunResults.ShortRuns; run++)
        {
            var clock = Stopwatch.StartNew();
            await using var app = new SpikeApp();
            var startupMs = clock.Elapsed.TotalMilliseconds;
            var page = await host.NewPageAsync(browser);

            await page.GotoAsync($"{app.BaseUrl}/widgets");
            var badgeColor = await page.Locator(".widget-badge").EvaluateAsync<string>("badge => getComputedStyle(badge).backgroundColor");
            await page.GotoAsync($"{app.BaseUrl}/wasm/counter");
            var interactive = await Tracker.InteractiveAsync(page);

            var passed = badgeColor == "rgb(0, 128, 0)" && interactive.Status == "ready";
            results.Add(passed, startupMs, $"badge color {badgeColor}; wasm interactive {interactive}; base URL {app.BaseUrl}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }
}
