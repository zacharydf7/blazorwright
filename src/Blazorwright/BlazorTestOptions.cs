namespace Blazorwright;

/// <summary>Settings for the Blazor waits: how long they may take, and which failures end them early.</summary>
public sealed record BlazorTestOptions
{
    public static BlazorTestOptions Default { get; } = new();

    /// <summary>How long a wait may take before it throws <see cref="BlazorTimeoutException"/>.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>How long nothing may change before the app counts as idle. It covers work no handler awaits.</summary>
    public TimeSpan QuietPeriod { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Failures that end a wait with <see cref="BlazorCircuitException"/> instead of letting it time out.</summary>
    public CircuitFailure FailOn { get; init; } = CircuitFailure.UnhandledException | CircuitFailure.Disconnected;
}
