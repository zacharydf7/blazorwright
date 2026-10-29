using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Issue #2: after an enhanced navigation, blazor.web.js adds the new page's root components only once the navigation
// has ended, so interactive has to wait for the fetch, the DOM patch and that update. blazor.server.js has no enhanced
// navigation, so its mode is left out.
public class EnhancedNavigationTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    [InlineData("chromium", "auto")]
    [InlineData("firefox", "auto")]
    public async Task Click_ShouldNotBeLost_WhenTestWaitsForInteractiveAfterEnhancedNavigation(string browser, string mode)
    {
        var results = new RunResults($"Nav-{browser}-{mode}");

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var page = await host.NewPageAsync(browser);
            await page.GotoAsync($"/{mode}/delayed");
            await Tracker.IdleAsync(page);
            await page.EvaluateAsync("() => window.beforeNavigation = true");

            var clock = Stopwatch.StartNew();
            await page.GetByRole(AriaRole.Link, new() { Name = "Counter page" }).ClickAsync();
            var interactive = await Tracker.InteractiveAsync(page);
            var interactiveMs = clock.Elapsed.TotalMilliseconds;
            var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
            var hasHandlers = await button.CountAsync() == 1 && await Tracker.HasHandlersAsync(button);
            await button.ClickAsync();
            await Tracker.IdleAsync(page);
            var text = await page.GetByRole(AriaRole.Status).TextContentAsync();
            var enhanced = await page.EvaluateAsync<bool>("() => window.beforeNavigation === true");

            var passed = interactive.Status == "ready" && hasHandlers && text == "Current count: 1" && enhanced;
            results.Add(passed, interactiveMs, $"text '{text}'; handlers {hasHandlers}; enhanced {enhanced}; interactive {interactive}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }
}
