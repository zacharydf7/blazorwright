using Microsoft.Playwright;

namespace Blazorwright.Xunit;

// Records a trace and a video for one test, then keeps them in a folder or deletes them.
internal sealed class ArtifactRecorder(FailureArtifacts artifacts)
{
    public string? VideoFolder { get; } = artifacts.HasFlag(FailureArtifacts.Video)
        ? Path.Combine(Path.GetTempPath(), "Blazorwright", Guid.NewGuid().ToString("N"))
        : null;

    public async Task StartAsync(IBrowserContext context)
    {
        if (artifacts.HasFlag(FailureArtifacts.Trace))
        {
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        }
    }

    // Closes the context, since Playwright finishes writing a video only then. Returns the files kept.
    public async Task<List<string>> StopAsync(IBrowserContext context, string? keepIn)
    {
        var kept = new List<string>();
        if (keepIn is not null)
        {
            Directory.CreateDirectory(keepIn);
        }

        if (artifacts.HasFlag(FailureArtifacts.Trace))
        {
            var trace = keepIn is null ? null : Path.Combine(keepIn, "trace.zip");
            await context.Tracing.StopAsync(new() { Path = trace });
            kept.AddRange(trace is null ? [] : [trace]);
        }

        await context.CloseAsync();
        if (VideoFolder is null || !Directory.Exists(VideoFolder))
        {
            return kept;
        }

        if (keepIn is not null)
        {
            var videos = Directory.GetFiles(VideoFolder);
            for (var index = 0; index < videos.Length; index++)
            {
                var keptVideo = Path.Combine(keepIn, $"video-{index + 1}{Path.GetExtension(videos[index])}");
                File.Move(videos[index], keptVideo, overwrite: true);
                kept.Add(keptVideo);
            }
        }

        Directory.Delete(VideoFolder, recursive: true);
        return kept;
    }
}
