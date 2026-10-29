namespace Blazorwright;

// What the tracker script reports when a wait ends: its status ("ready", "timeout" or "failed") and what was still busy.
internal sealed record TrackerSnapshot(string Status, Dictionary<string, int> DotNetCalls, Dictionary<string, int> ParkedCalls,
    int PendingBatches, string[] PendingRootComponents, bool Navigating, double MsSinceActivity, string? Failure,
    string? FailureKind)
{
    public CircuitFailure FailureFlag => Tracker.FailureFlag(FailureKind);

    public string DescribeBusy() => Join(
        Calls(DotNetCalls, "in flight"),
        ParkedCalls.Count > 0 ? Calls(ParkedCalls, "waiting on a dialog") : null,
        Batches(),
        RootComponents(),
        Navigation(),
        $"last activity {MsSinceActivity:F0} ms ago");

    public string DescribeNotInteractive() => Join(RootComponents(), PendingBatches > 0 ? Batches() : null, Navigation());

    private string Batches() => PendingBatches == 1 ? "1 render batch pending" : $"{PendingBatches} render batches pending";

    private string? RootComponents() => PendingRootComponents.Length > 0
        ? $"root components still attaching ({string.Join(", ", PendingRootComponents)})"
        : null;

    private string? Navigation() => Navigating ? "an enhanced navigation in progress" : null;

    private static string Calls(Dictionary<string, int> calls, string state)
    {
        var total = calls.Values.Sum();
        var names = string.Join(", ", calls.Select(call => call.Value == 1 ? call.Key : $"{call.Key} x{call.Value}"));
        return total switch
        {
            0 => $"no calls {state}",
            1 => $"1 call {state} ({names})",
            _ => $"{total} calls {state} ({names})",
        };
    }

    private static string Join(params string?[] parts) => string.Join(", ", parts.OfType<string>());
}
