using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q4: a click straight after GotoAsync hits the prerendered button before its handler is attached, unless the test
// waits for interactivity first. The host delays circuit start by 300 ms; on localhost the circuit otherwise attaches
// before Playwright's click lands.
public class Q4InteractiveTests(SlowCircuitHost host) : IClassFixture<SlowCircuitHost>
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
    public async Task Click_ShouldNotBeLost_WhenTestWaitsForInteractive(string browser, string mode)
    {
        var results = new RunResults($"Q4-wait-{browser}-{mode}");

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var page = await host.NewPageAsync(browser);
            var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
            await page.GotoAsync($"/{mode}/counter");

            var clock = Stopwatch.StartNew();
            var interactive = await Tracker.InteractiveAsync(page);
            var interactiveMs = clock.Elapsed.TotalMilliseconds;
            var hasHandlers = await Tracker.HasHandlersAsync(button);
            await button.ClickAsync();
            await Tracker.IdleAsync(page);
            var text = await page.GetByRole(AriaRole.Status).TextContentAsync();

            var passed = interactive.Status == "ready" && hasHandlers && text == "Current count: 1"
                && (mode != "serverjs" || interactive.UsedServerScript);
            results.Add(passed, interactiveMs, $"text '{text}'; handlers {hasHandlers}; interactive {interactive}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }
}
