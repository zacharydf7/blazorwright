using Microsoft.Playwright;

namespace Spike.Tests;

// Q4, the other half: without the wait the click is lost, which proves a test can fail. Firefox with WebAssembly is
// left out because Firefox holds Playwright's click until the runtime has attached, so no click is ever lost there.
public class Q4LostClickTests(SlowCircuitHost host) : IClassFixture<SlowCircuitHost>
{
    [Theory]
    [InlineData("chromium", "server")]
    [InlineData("firefox", "server")]
    [InlineData("chromium", "serverjs")]
    [InlineData("firefox", "serverjs")]
    [InlineData("chromium", "wasm")]
    [InlineData("chromium", "auto")]
    [InlineData("firefox", "auto")]
    public async Task Click_ShouldBeLostSometimes_WhenTestClicksStraightAfterGoto(string browser, string mode)
    {
        var results = new RunResults($"Q4-nowait-{browser}-{mode}");

        for (var run = 0; run < RunResults.Runs; run++)
        {
            var page = await host.NewPageAsync(browser);
            var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
            await page.GotoAsync($"/{mode}/counter");

            var hasHandlers = await Tracker.HasHandlersAsync(button);
            await button.ClickAsync();
            var idle = await Tracker.IdleAsync(page);
            var text = await page.GetByRole(AriaRole.Status).TextContentAsync();

            // Here "passed" means the click was lost, which proves the test can fail without the wait.
            var lost = text == "Current count: 0" && (mode != "serverjs" || idle.UsedServerScript);
            results.Add(lost, 0, $"text '{text}'; handlers at click {hasHandlers}; idle {idle}");
            await page.Context.CloseAsync();
        }

        results.Write();
        Assert.True(results.Passed > 0, "No click was lost, so this page cannot show the need for an interactive wait.");
    }
}
