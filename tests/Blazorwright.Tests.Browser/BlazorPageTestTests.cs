using Blazorwright.Xunit;
using Microsoft.Playwright;

namespace Blazorwright.Tests.Browser;

public class BlazorPageTestTests(SlowCircuitAppFixture app) : BlazorPageTest(app), IClassFixture<SlowCircuitAppFixture>
{
    protected override FailureArtifacts Artifacts => FailureArtifacts.None;

    [Fact]
    public async Task Page_ShouldOpenAppPathsWithTrackingInstalled_WhenTestStarts()
    {
        await Page.GotoBlazorAsync("/server/counter");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        await Page.WaitForBlazorIdleAsync();

        Assert.Equal("Current count: 1", await Page.GetByRole(AriaRole.Status).TextContentAsync());
    }
}
