using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace Blazorwright.Xunit;

/// <summary>
/// A test with a Playwright <see cref="Page"/> that has Blazor tracking installed and opens paths relative to the app.
/// A failed test keeps its trace and video under TestResults/Blazor/&lt;test&gt;/.
/// </summary>
public class BlazorPageTest(IBlazorApp app) : ContextTest
{
    private ArtifactRecorder _recorder = null!;

    public IPage Page { get; private set; } = null!;

    /// <summary>The defaults for this class's Blazor waits.</summary>
    protected virtual BlazorTestOptions Options => BlazorTestOptions.Default;

    protected virtual FailureArtifacts Artifacts => FailureArtifacts.Trace | FailureArtifacts.Video;

    public override BrowserNewContextOptions ContextOptions() =>
        new(base.ContextOptions()) { BaseURL = app.BaseUrl, RecordVideoDir = _recorder.VideoFolder };

    public override async ValueTask InitializeAsync()
    {
        _recorder = new ArtifactRecorder(Artifacts);
        await base.InitializeAsync();
        await Context.AddBlazorTrackingAsync(Options, app.Log);
        await _recorder.StartAsync(Context);
        Page = await Context.NewPageAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        // Context is null when setup failed before it opened; the setup error is the one to report.
        if (Context is not null)
        {
            var kept = await _recorder.StopAsync(Context, TestOk() ? null : FailureFolder());
            foreach (var file in kept)
            {
                TestContext.Current.TestOutputHelper?.WriteLine($"Kept {file}");
            }
        }

        await base.DisposeAsync();
    }

    private static string FailureFolder()
    {
        var name = TestContext.Current.Test?.TestDisplayName ?? "test";
        var safeName = string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(AppContext.BaseDirectory, "TestResults", "Blazor", safeName[..Math.Min(safeName.Length, 120)]);
    }
}
