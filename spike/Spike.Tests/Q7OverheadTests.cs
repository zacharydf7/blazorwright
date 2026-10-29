using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q7: idle on a page that is already idle returns within the quiet period, and the burst records its duration with and
// without the tracker.
public class Q7OverheadTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    private const string BurstDone = "() => document.querySelector('[role=status]')?.textContent === 'Burst done'";
    private const string ButtonHasHandlers =
        "() => Object.keys(document.querySelector('button') ?? {}).some(key => key.startsWith('_blazorEvents_'))";

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Idle_ShouldReturnWithinQuietPeriod_WhenPageIsAlreadyIdle(string browser)
    {
        var results = new RunResults($"Q7-idle-on-idle-{browser}");
        var page = await host.NewPageAsync(browser);
        await page.GotoAsync("/server/counter");
        await Tracker.IdleAsync(page);

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var clock = Stopwatch.StartNew();
            var idle = await Tracker.IdleAsync(page);
            var latencyMs = clock.Elapsed.TotalMilliseconds;

            results.Add(idle.Status == "ready" && latencyMs <= Tracker.QuietMs + 50, latencyMs, $"idle {idle}");
        }

        await page.Context.CloseAsync();
        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }

    // A measurement, not a check: plain and tracker runs alternate so both see the same machine load.
    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Burst_ShouldRecordDurationWithAndWithoutTracker_WhenRunsAlternate(string browser)
    {
        var plain = new RunResults($"Q7-burst-{browser}-plain");
        var tracked = new RunResults($"Q7-burst-{browser}-tracker");

        for (var run = 0; run < RunResults.ShortRuns; run++)
        {
            plain.Add(true, await BurstMsAsync(browser, withTracker: false), "");
            tracked.Add(true, await BurstMsAsync(browser, withTracker: true), "");
        }

        plain.Write();
        tracked.Write();
    }

    private async Task<double> BurstMsAsync(string browser, bool withTracker)
    {
        var page = await host.NewPageAsync(browser, withTracker);
        await page.GotoAsync("/server/updates");
        await page.WaitForFunctionAsync(ButtonHasHandlers);

        var clock = Stopwatch.StartNew();
        await page.GetByRole(AriaRole.Button, new() { Name = "Burst" }).ClickAsync();
        await page.WaitForFunctionAsync(BurstDone);
        var burstMs = clock.Elapsed.TotalMilliseconds;
        await page.Context.CloseAsync();
        return burstMs;
    }
}
