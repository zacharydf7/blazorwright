using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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

    private static readonly ConditionalWeakTable<IBrowserContext, Registration> Registrations = new();

    public static string Script { get; } = ReadScript();

    public static void Register(IBrowserContext context, BlazorTestOptions options, ServerLog? serverLog) =>
        Registrations.AddOrUpdate(context, new Registration(options, serverLog, serverLog?.Position ?? 0));

    public static BlazorTestOptions OptionsFor(IPage page, BlazorTestOptions? options) => options
        ?? (Registrations.TryGetValue(page.Context, out var registration) ? registration.Options : BlazorTestOptions.Default);

    // Appends the warnings and errors the server logged since the page's context was registered.
    public static string WithServerLog(IPage page, string message)
    {
        if (!Registrations.TryGetValue(page.Context, out var registration) || registration.ServerLog is null)
        {
            return message;
        }

        var lines = registration.ServerLog.Since(registration.LogPosition)
            .Where(entry => entry.Level >= LogLevel.Warning)
            .Select(entry => entry.ToString())
            .ToList();
        return lines.Count > 0 ? string.Join(Environment.NewLine, [message, "", "Server log:", .. lines]) : message;
    }

    public static NotSupportedException NotInstalled() => new("Blazor tracking isn't installed on this page. "
        + "Call AddBlazorTrackingAsync on its browser context before opening the page.");

    public static string Seconds(TimeSpan time) => $"{time.TotalSeconds:0.###} s";

    public static async Task WaitAsync(IPage page, string wait, BlazorTestOptions? options)
    {
        var resolved = OptionsFor(page, options);
        var snapshot = await EvaluateAsync(page, wait, resolved) ?? throw NotInstalled();
        if (snapshot.Status == "failed")
        {
            throw new BlazorCircuitException(snapshot.FailureFlag,
                WithServerLog(page, $"The app failed while waiting for it to be {wait}: {snapshot.Failure}"));
        }

        if (snapshot.Status == "timeout")
        {
            var busy = wait == Idle ? snapshot.DescribeBusy() : snapshot.DescribeNotInteractive();
            throw new BlazorTimeoutException(WithServerLog(page, $"Not {wait} after {Seconds(resolved.Timeout)}: {busy}."));
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

    private sealed record Registration(BlazorTestOptions Options, ServerLog? ServerLog, int LogPosition);

    private static string ReadScript()
    {
        using var stream = typeof(Tracker).Assembly.GetManifestResourceStream("Blazorwright.tracker.js")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
