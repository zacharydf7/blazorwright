using System.Text.Json;
using Microsoft.Playwright;

namespace Spike.Tests;

public record TrackerSnapshot(string Status, Dictionary<string, int> DotNetCalls, int PendingBatches, int LastBatchReceived,
    int LastBatchCompleted, string[] PendingRootComponents, double MsSinceActivity, double ElapsedMs, string? Failure,
    int DecodeErrors, Dictionary<string, int> RootUpdates, string[] MarkerTypes)
{
    // blazor.server.js attaches roots without ever sending UpdateRootComponents.
    public bool UsedServerScript => RootUpdates["serverSent"] == 0 && RootUpdates["serverAttached"] > 0;

    public override string ToString() =>
        $"{Status} after {ElapsedMs:F0} ms: calls [{string.Join(", ", DotNetCalls.Select(call => $"{call.Key} x{call.Value}"))}], "
        + $"batches {LastBatchCompleted}/{LastBatchReceived}, pending roots [{string.Join(", ", PendingRootComponents)}], "
        + $"quiet {MsSinceActivity:F0} ms, failure {Failure ?? "none"}, decode errors {DecodeErrors}, "
        + $"root updates [{string.Join(", ", RootUpdates.Select(update => $"{update.Key} {update.Value}"))}], "
        + $"markers [{string.Join(", ", MarkerTypes)}]";
}

// Thin wrappers over the tracker's page API.
public static class Tracker
{
    public const int QuietMs = 100;
    public const int TimeoutMs = 15_000;

    public static Task<TrackerSnapshot> IdleAsync(IPage page, int quietMs = QuietMs, int timeoutMs = TimeoutMs) =>
        EvaluateAsync(page, "([quiet, timeout]) => window.__blazorTest.idle(quiet, timeout)", new[] { quietMs, timeoutMs });

    public static Task<TrackerSnapshot> InteractiveAsync(IPage page, int timeoutMs = TimeoutMs) =>
        EvaluateAsync(page, "timeout => window.__blazorTest.interactive(timeout)", timeoutMs);

    public static Task<TrackerSnapshot> SnapshotAsync(IPage page) =>
        EvaluateAsync(page, "() => window.__blazorTest.snapshot()", null);

    public static Task<bool> HasHandlersAsync(ILocator element) =>
        element.EvaluateAsync<bool>("element => window.__blazorTest.hasHandlers(element)");

    // Playwright maps results only onto types with a parameterless constructor, so snapshots come back as JSON.
    private static async Task<TrackerSnapshot> EvaluateAsync(IPage page, string script, object? argument)
    {
        var json = await page.EvaluateAsync<string>($"async argument => JSON.stringify(await ({script})(argument))", argument);
        return JsonSerializer.Deserialize<TrackerSnapshot>(json, JsonSerializerOptions.Web)!;
    }
}
