using System.Text.Json;

namespace Spike.Tests;

public record RunResult(int Run, bool Passed, double LatencyMs, string Detail);

// Collects one spike's runs and writes a summary line and a JSON file under spike-results/.
public class RunResults(string name)
{
    // Q5's pass bar is 20 runs; Q6 and the Q7 burst reuse it because the 500-run count is only for Q1, Q2 and Q4.
    public const int ShortRuns = 20;

    private static readonly Lock SummaryLock = new();
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "spike-results");
    private readonly List<RunResult> _runs = [];

    static RunResults()
    {
        Directory.CreateDirectory(Folder);
        File.Delete(Path.Combine(Folder, "summary.txt"));
    }

    public static int Runs => RunsFrom("SPIKE_RUNS");

    public static int SweepRuns => RunsFrom("SPIKE_SWEEP_RUNS");

    public int Count => _runs.Count;

    public int Passed => _runs.Count(run => run.Passed);

    public void Add(bool passed, double latencyMs, string detail) =>
        _runs.Add(new RunResult(_runs.Count + 1, passed, latencyMs, detail));

    public string Summary()
    {
        var latencies = _runs.Select(run => run.LatencyMs).Order().ToList();
        var firstFailure = _runs.FirstOrDefault(run => !run.Passed);
        return $"{name}: {Passed}/{Count} passed; latency ms p50 {Percentile(latencies, 0.50):F0}, "
            + $"p95 {Percentile(latencies, 0.95):F0}, max {latencies.LastOrDefault():F0}"
            + (firstFailure is null ? "" : $"; first failure run {firstFailure.Run}: {firstFailure.Detail}");
    }

    public void Write()
    {
        var json = JsonSerializer.Serialize(new { name, summary = Summary(), runs = _runs });
        File.WriteAllText(Path.Combine(Folder, $"{name}.json"), json);
        lock (SummaryLock)
        {
            File.AppendAllText(Path.Combine(Folder, "summary.txt"), Summary() + Environment.NewLine);
        }

        TestContext.Current.TestOutputHelper?.WriteLine(Summary());
    }

    private static int RunsFrom(string variable) =>
        int.TryParse(Environment.GetEnvironmentVariable(variable), out var runs) ? runs : 10;

    private static double Percentile(List<double> sorted, double fraction) =>
        sorted.Count == 0 ? 0 : sorted[(int)Math.Min(sorted.Count - 1, Math.Ceiling(fraction * sorted.Count) - 1)];
}
