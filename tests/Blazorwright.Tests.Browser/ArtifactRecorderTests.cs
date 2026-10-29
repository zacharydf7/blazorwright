using Blazorwright.Xunit;

namespace Blazorwright.Tests.Browser;

public class ArtifactRecorderTests(Browsers browsers, TestAppFixture app) : IClassFixture<TestAppFixture>
{
    [Fact]
    public async Task StopAsync_ShouldKeepTraceAndVideo_WhenGivenFolder()
    {
        var recorder = new ArtifactRecorder(FailureArtifacts.Trace | FailureArtifacts.Video);
        var keepIn = Path.Combine(Path.GetTempPath(), "Blazorwright.Tests", Guid.NewGuid().ToString("N"));
        var context = await RecordPageAsync(recorder);

        var kept = await recorder.StopAsync(context, keepIn);

        Assert.Equal([Path.Combine(keepIn, "trace.zip"), Path.Combine(keepIn, "video-1.webm")], kept);
        Assert.All(kept, file => Assert.True(new FileInfo(file).Length > 0));
        Assert.False(Directory.Exists(recorder.VideoFolder));
        Directory.Delete(keepIn, recursive: true);
    }

    [Fact]
    public async Task StopAsync_ShouldDeleteEverything_WhenGivenNoFolder()
    {
        var recorder = new ArtifactRecorder(FailureArtifacts.Trace | FailureArtifacts.Video);
        var context = await RecordPageAsync(recorder);

        var kept = await recorder.StopAsync(context, null);

        Assert.Empty(kept);
        Assert.False(Directory.Exists(recorder.VideoFolder));
    }

    private async Task<Microsoft.Playwright.IBrowserContext> RecordPageAsync(ArtifactRecorder recorder)
    {
        var context = await browsers.NewContextAsync("chromium", new() { BaseURL = app.BaseUrl, RecordVideoDir = recorder.VideoFolder });
        await recorder.StartAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/server/counter");
        return context;
    }
}
