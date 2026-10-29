using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Playwright;

namespace Blazorwright;

// Runs the waits in tracker.js, which the browser context installs before any page script.
internal static class Tracker
{
    public const string Idle = "idle";
    public const string Interactive = "interactive";

    private const string WaitScript = """
        async ([wait, quietMs, timeoutMs, failOn]) => {
            const tracker = window.__blazorTest;
            if (!tracker) {
                return null;
            }
            const snapshot = wait === 'idle'
                ? await tracker.idle(quietMs, timeoutMs, failOn)
                : await tracker.interactive(timeoutMs, failOn);
            return JSON.stringify(snapshot);
        }
        """;

    private static readonly ConditionalWeakTable<IBrowserContext, BlazorTestOptions> ContextOptions = new();

    public static string Script { get; } = ReadScript();

    public static void Register(IBrowserContext context, BlazorTestOptions options) => ContextOptions.AddOrUpdate(context, options);

    public static BlazorTestOptions OptionsFor(IPage page, BlazorTestOptions? options) =>
        options ?? (ContextOptions.TryGetValue(page.Context, out var registered) ? registered : BlazorTestOptions.Default);

    public static NotSupportedException NotInstalled() =>
        new("Blazor tracking isn't installed on this page. Call AddBlazorTrackingAsync on its browser context before opening the page.");

    public static string Seconds(TimeSpan time) => $"{time.TotalSeconds:0.###} s";

    public static async Task WaitAsync(IPage page, string wait, BlazorTestOptions? options)
    {
        var resolved = OptionsFor(page, options);
        var snapshot = await EvaluateAsync(page, wait, resolved) ?? throw NotInstalled();
        if (snapshot.Status == "failed")
        {
            throw new BlazorCircuitException(snapshot.FailureFlag,
                $"The app failed while waiting for it to be {wait}: {snapshot.Failure}");
        }

        if (snapshot.Status == "timeout")
        {
            var busy = wait == Idle ? snapshot.DescribeBusy() : snapshot.DescribeNotInteractive();
            throw new BlazorTimeoutException($"Not {wait} after {Seconds(resolved.Timeout)}: {busy}.");
        }
    }

    // A full page load destroys the page's script context mid-wait; the wait then starts again on the new page.
    private static async Task<TrackerSnapshot?> EvaluateAsync(IPage page, string wait, BlazorTestOptions options)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            var timeLeft = Math.Max(0, (options.Timeout - clock.Elapsed).TotalMilliseconds);
            try
            {
                var json = await page.EvaluateAsync<string?>(WaitScript,
                    new object[] { wait, options.QuietPeriod.TotalMilliseconds, timeLeft, FailureKinds(options.FailOn) });
                return json is null ? null : JsonSerializer.Deserialize<TrackerSnapshot>(json, JsonSerializerOptions.Web);
            }
            catch (PlaywrightException exception) when (exception.Message.Contains("Execution context was destroyed") && timeLeft > 0)
            {
                await page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = (float)timeLeft });
            }
        }
    }

    private static List<string> FailureKinds(CircuitFailure failOn)
    {
        var kinds = new List<string>();
        if (failOn.HasFlag(CircuitFailure.UnhandledException))
        {
            kinds.Add("unhandledException");
        }

        if (failOn.HasFlag(CircuitFailure.Disconnected))
        {
            kinds.Add("disconnected");
        }

        return kinds;
    }

    private static string ReadScript()
    {
        using var stream = typeof(Tracker).Assembly.GetManifestResourceStream("Blazorwright.tracker.js")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
