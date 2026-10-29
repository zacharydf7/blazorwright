using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Q6: a handler that throws ends the wait quickly, and the server log holds the exception.
public class Q6FailFastTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    private const int FailFastLimitMs = 2000;

    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task Idle_ShouldFailFast_WhenHandlerThrows(string browser, string mode)
    {
        var results = new RunResults($"Q6-{browser}-{mode}");

        for (var run = 0; run < RunResults.ShortRuns; run++)
        {
            var page = await host.NewPageAsync(browser);
            await page.GotoAsync($"/{mode}/throws");
            await Tracker.IdleAsync(page);
            var logPosition = host.App.Log.Position;

            var clock = Stopwatch.StartNew();
            await page.GetByRole(AriaRole.Button, new() { Name = "Throw" }).ClickAsync();
            var idle = await Tracker.IdleAsync(page);
            var clickToFailureMs = clock.Elapsed.TotalMilliseconds;
            var serverException = host.App.Log.Since(logPosition)
                .FirstOrDefault(entry => entry.Exception?.Message == "The throw button was clicked.");

            // WebAssembly handlers throw in the browser, so only Server has a server-side exception to attach.
            var passed = idle.Status == "failed" && clickToFailureMs < FailFastLimitMs
                && (mode == "wasm" || serverException is not null);
            results.Add(passed, clickToFailureMs, $"server exception {serverException?.Category ?? "none"}; idle {idle}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }
}
