namespace Blazorwright;

[Flags]
public enum CircuitFailure
{
    None = 0,

    /// <summary>The server reported an unhandled exception, or Blazor showed its error UI.</summary>
    UnhandledException = 1,

    /// <summary>The circuit's connection closed unexpectedly, or reconnecting failed.</summary>
    Disconnected = 2,
}
