namespace Blazorwright.Xunit;

/// <summary>What a failed test keeps under TestResults/Blazor/&lt;test&gt;/. Passing tests keep nothing.</summary>
[Flags]
public enum FailureArtifacts
{
    None = 0,

    /// <summary>A Playwright trace with screenshots and DOM snapshots; open it with <c>playwright show-trace</c>.</summary>
    Trace = 1,

    /// <summary>A video of the page.</summary>
    Video = 2,
}
