namespace Blazorwright;

// What the tracker script reports when a wait ends: its status ("ready", "timeout" or "failed") and what was still busy.
internal sealed record TrackerSnapshot(string Status, Dictionary<string, int> DotNetCalls, Dictionary<string, int> ParkedCalls,
    int PendingBatches, string[] PendingRootComponents, bool Navigating, double MsSinceActivity, string? Failure,
    string? FailureKind, RecentChange[] RecentChanges)
{
    public CircuitFailure FailureFlag => Tracker.FailureFlag(FailureKind);

    // Recent changes arrive only when a wait timed out with nothing busy, so any here mean the page kept changing.
    public string DescribeBusy() => RecentChanges.Length > 0 ? DescribeChanges() : Join(
        Calls(DotNetCalls, "in flight"),
        ParkedCalls.Count > 0 ? Calls(ParkedCalls, "waiting on a dialog") : null,
        Batches(),
        RootComponents(),
        Navigation(),
        $"last activity {MsSinceActivity:F0} ms ago");

    private string DescribeChanges() => Join(
        "nothing was busy, but the page never went quiet; in the last second: "
            + string.Join<RecentChange>(", ", RecentChanges),
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

// An element the page changed, how (text, children or an attribute) and how often in the last second.
internal sealed record RecentChange(string Target, string Kind, int Count)
{
    public override string ToString() =>
        $"{Target} ({Kind}, {(Count == 1 ? "once" : $"{Count} times")})";
}
