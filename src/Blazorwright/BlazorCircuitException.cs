namespace Blazorwright;

/// <summary>The app failed during a Blazor wait, so the wait stopped instead of timing out.</summary>
public class BlazorCircuitException(CircuitFailure failure, string message) : Exception(message)
{
    public CircuitFailure Failure { get; } = failure;
}
