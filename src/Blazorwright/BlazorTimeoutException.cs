namespace Blazorwright;

/// <summary>A Blazor wait ran out of time. The message says what was still running.</summary>
public class BlazorTimeoutException(string message) : TimeoutException(message);
