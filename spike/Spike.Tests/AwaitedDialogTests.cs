using System.Diagnostics;
using Microsoft.Playwright;

namespace Spike.Tests;

// Issue #4: a click handler that awaits a dialog's result stays in flight until the dialog closes. Idle has to return
// while the dialog is open, then wait for the handler's work after it closes (a 300 ms save).
public class AwaitedDialogTests(SpikeHost host) : IClassFixture<SpikeHost>
{
    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "serverjs")]
    [InlineData("firefox", "serverjs")]
    [InlineData("chromium", "wasm")]
    [InlineData("firefox", "wasm")]
    public async Task Idle_ShouldReturnWhileDialogIsOpen_WhenHandlerAwaitsDialogResult(string browser, string mode)
    {
        var results = new RunResults($"Dialog-{browser}-{mode}");

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var page = await host.NewPageAsync(browser);
            await page.GotoAsync($"/{mode}/rename");
            await Tracker.IdleAsync(page);

            await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
            var opened = await Tracker.IdleAsync(page);
            var dialogVisible = await page.GetByRole(AriaRole.Dialog).IsVisibleAsync();
            await page.GetByLabel("Name", new() { Exact = true }).FillAsync("Ada");
            var clock = Stopwatch.StartNew();
            await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
            var saved = await Tracker.IdleAsync(page);
            var saveToIdleMs = clock.Elapsed.TotalMilliseconds;
            var text = await page.GetByRole(AriaRole.Status).TextContentAsync();

            var passed = opened.Status == "ready" && opened.ParkedCalls.Count > 0 && dialogVisible
                && saved.Status == "ready" && saved.ParkedCalls.Count == 0 && text == "Saved Ada";
            results.Add(passed, saveToIdleMs, $"text '{text}'; dialog visible {dialogVisible}; opened {opened}; saved {saved}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.Equal(results.Count, results.Passed);
    }
}
