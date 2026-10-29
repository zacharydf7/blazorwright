using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q1: idle waits for an event handler's awaited work (await Task.Delay(800), then the text changes).
public class Q1AwaitedHandlerTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "serverjs")]
    [InlineData("firefox", "serverjs")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task Idle_ShouldReturnAfterAwaitedWork_WhenHandlerAwaitsDelay(string browser, string mode)
    {
        var results = new RunResults($"Q1-{browser}-{mode}");

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var page = await host.NewPageAsync(browser);
            await page.GotoAsync($"/{mode}/delayed");
            await Tracker.IdleAsync(page);

            var clock = Stopwatch.StartNew();
            await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
            var afterClick = await Tracker.SnapshotAsync(page);
            var idle = await Tracker.IdleAsync(page);
            var clickToIdleMs = clock.Elapsed.TotalMilliseconds;
            var text = await page.GetByRole(AriaRole.Status).TextContentAsync();

            var passed = text == "Loaded" && afterClick.DotNetCalls.Count > 0 && idle.Status == "ready"
                && (mode != "serverjs" || idle.UsedServerScript);
            results.Add(passed, clickToIdleMs, $"text '{text}'; after click {afterClick}; idle {idle}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }
}
