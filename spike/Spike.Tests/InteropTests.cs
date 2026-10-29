using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Issue #3: fire-and-forget work that awaits a 200 ms JS call renders after a round trip the call and batch counts can't
// see unless .NET-to-JS calls count too. This sweeps the quiet period the way Q3 does.
public class InteropTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    private static readonly int[] QuietPeriods = [0, 25, 50, 100];

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "serverjs")]
    [InlineData("firefox", "serverjs")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task Idle_ShouldWaitForJSCall_WhenFireAndForgetWorkAwaitsIt(string browser, string mode)
    {
        RunResults? defaultPeriodResults = null;

        foreach (var quietMs in QuietPeriods)
        {
            var results = new RunResults($"Interop-{browser}-{mode}-quiet{quietMs}");
            for (var run = 0; run < RunResults.SweepRuns; run++)
            {
                var page = await host.NewPageAsync(browser);
                await page.GotoAsync($"/{mode}/measure");
                await Tracker.IdleAsync(page);

                var clock = Stopwatch.StartNew();
                await page.GetByRole(AriaRole.Button, new() { Name = "Show tip" }).ClickAsync();
                var idle = await Tracker.IdleAsync(page, quietMs);
                var clickToIdleMs = clock.Elapsed.TotalMilliseconds;
                var text = await page.GetByRole(AriaRole.Status).TextContentAsync();

                results.Add(text == "Width 240" && idle.Status == "ready", clickToIdleMs, $"text '{text}'; idle {idle}");
                await page.Context.CloseAsync();
            }

            results.Write();
            if (quietMs == Tracker.QuietMs)
            {
                defaultPeriodResults = results;
            }
        }

        Assert.NotNull(defaultPeriodResults);
        Assert.Equal(defaultPeriodResults.Count, defaultPeriodResults.Passed);
    }
}
