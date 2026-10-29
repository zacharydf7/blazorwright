using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q3: work a handler does not await is invisible, so only the quiet period covers it. This measures how long a quiet
// period the sample dropdown (50 ms delay) and toggle (10 ms background hop) need.
public class Q3FireAndForgetTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    private static readonly int[] QuietPeriods = [0, 25, 50, 75, 100, 150];

    [Theory]
    [InlineData("chromium", "server", "dropdown")]
    [InlineData("firefox", "server", "dropdown")]
    [InlineData("chromium", "wasm", "dropdown")]
    [InlineData("firefox", "wasm", "dropdown")]
    [InlineData("chromium", "server", "toggle")]
    [InlineData("firefox", "server", "toggle")]
    [InlineData("chromium", "wasm", "toggle")]
    [InlineData("firefox", "wasm", "toggle")]
    public async Task Idle_ShouldCoverFireAndForgetWork_WhenQuietPeriodIsDefault(string browser, string mode, string name)
    {
        RunResults? defaultPeriodResults = null;

        foreach (var quietMs in QuietPeriods)
        {
            var results = new RunResults($"Q3-{name}-{browser}-{mode}-quiet{quietMs}");
            for (var run = 0; run < RunResults.SweepRuns; run++)
            {
                var page = await host.NewPageAsync(browser);
                await page.GotoAsync($"/{mode}/{name}");
                await Tracker.IdleAsync(page);

                var clock = Stopwatch.StartNew();
                await page.GetByRole(AriaRole.Button).ClickAsync();
                var idle = await Tracker.IdleAsync(page, quietMs);
                var clickToIdleMs = clock.Elapsed.TotalMilliseconds;
                var done = name == "dropdown"
                    ? await page.GetByRole(AriaRole.Listbox).CountAsync() == 1
                    : await page.GetByRole(AriaRole.Status).TextContentAsync() == "On";

                results.Add(done && idle.Status == "ready", clickToIdleMs, $"done {done}; idle {idle}");
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
