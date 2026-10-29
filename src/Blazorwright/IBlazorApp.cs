namespace Blazorwright;

/// <summary>An app the tests drive: where it listens, and its log when it runs in the test process.</summary>
public interface IBlazorApp
{
    string BaseUrl { get; }

    ServerLog? Log { get; }
}
