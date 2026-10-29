namespace Blazorwright.Tests.Unit;

public class TrackerSnapshotTests
{
    [Fact]
    public void DescribeBusy_ShouldNameEachCall_WhenCallsAreInFlight()
    {
        var snapshot = Snapshot() with { DotNetCalls = new() { ["DispatchEventAsync"] = 2, ["JS app.measure"] = 1 } };

        var description = snapshot.DescribeBusy();

        Assert.Equal("3 calls in flight (DispatchEventAsync x2, JS app.measure), 0 render batches pending, "
            + "last activity 40 ms ago", description);
    }

    [Fact]
    public void DescribeBusy_ShouldListEveryReason_WhenEverythingIsBusy()
    {
        var snapshot = Snapshot() with
        {
            ParkedCalls = new() { ["DispatchEventAsync"] = 1 },
            PendingBatches = 1,
            PendingRootComponents = ["server"],
            Navigating = true,
        };

        var description = snapshot.DescribeBusy();

        Assert.Equal("no calls in flight, 1 call waiting on a dialog (DispatchEventAsync), 1 render batch pending, "
            + "root components still attaching (server), an enhanced navigation in progress, last activity 40 ms ago",
            description);
    }

    [Fact]
    public void DescribeNotInteractive_ShouldListOnlyWhatBlocksInteractivity_WhenRootComponentsAreAttaching()
    {
        var snapshot = Snapshot() with
        {
            DotNetCalls = new() { ["DispatchEventAsync"] = 1 },
            PendingRootComponents = ["server", "webassembly"],
        };

        var description = snapshot.DescribeNotInteractive();

        Assert.Equal("root components still attaching (server, webassembly)", description);
    }

    [Theory]
    [InlineData("unhandledException", CircuitFailure.UnhandledException)]
    [InlineData("disconnected", CircuitFailure.Disconnected)]
    [InlineData("tracker", CircuitFailure.None)]
    public void FailureFlag_ShouldMatchFailureKind_WhenGivenTrackerKind(string kind, CircuitFailure expected)
    {
        var snapshot = Snapshot() with { FailureKind = kind };

        Assert.Equal(expected, snapshot.FailureFlag);
    }

    private static TrackerSnapshot Snapshot() =>
        new("timeout", [], [], 0, [], false, 40, null, null);
}
