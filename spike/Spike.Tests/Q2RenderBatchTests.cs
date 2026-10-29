using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q2: idle never returns while the server has sent a render batch the browser has not applied.
public partial class Q2RenderBatchTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    [Theory]
    [InlineData("chromium", "server", "Run", "Done by service")]
    [InlineData("firefox", "server", "Run", "Done by service")]
    [InlineData("chromium", "server", "Burst", "Burst done")]
    [InlineData("firefox", "server", "Burst", "Burst done")]
    [InlineData("chromium", "serverjs", "Run", "Done by service")]
    [InlineData("firefox", "serverjs", "Run", "Done by service")]
    [InlineData("chromium", "serverjs", "Burst", "Burst done")]
    [InlineData("firefox", "serverjs", "Burst", "Burst done")]
    public async Task Idle_ShouldReturnAfterLastBatchIsApplied_WhenServerRendersRepeatedly(string browser, string mode,
        string button, string finalStatus)
    {
        var results = new RunResults($"Q2-{browser}-{mode}-{button}");

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var page = await host.NewPageAsync(browser);
            await page.GotoAsync($"/{mode}/updates");
            await Tracker.IdleAsync(page);
            var logPosition = host.App.Log.Position;

            var clock = Stopwatch.StartNew();
            await page.GetByRole(AriaRole.Button, new() { Name = button }).ClickAsync();
            var idle = await Tracker.IdleAsync(page);
            var clickToIdleMs = clock.Elapsed.TotalMilliseconds;
            var serverLog = host.App.Log.Since(logPosition);
            var lastBatchSent = LastBatchSent(serverLog);
            var queueFullCount = serverLog.Count(entry => entry.Message.Contains("unacknowledged render batches is full"));
            var status = await page.GetByRole(AriaRole.Status).TextContentAsync();

            var passed = idle.Status == "ready" && status == finalStatus && lastBatchSent > 0
                && idle.LastBatchCompleted >= lastBatchSent && (mode != "serverjs" || idle.UsedServerScript);
            results.Add(passed, clickToIdleMs, $"status '{status}'; server sent up to batch {lastBatchSent}, "
                + $"queue full {queueFullCount} times; idle {idle}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }

    private static int LastBatchSent(List<ServerLogEntry> entries) =>
        entries.Where(entry => entry.Category == SpikeApp.RendererCategory)
            .Select(entry => SentBatch().Match(entry.Message))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

    [GeneratedRegex(@"Sending render batch (\d+)")]
    private static partial Regex SentBatch();
}
