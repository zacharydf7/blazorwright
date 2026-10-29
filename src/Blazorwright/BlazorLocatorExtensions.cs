using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace Blazorwright;

public static class BlazorLocatorExtensions
{
    // EvaluateAllAsync doesn't wait for a match, so the app's failures are checked even while the element is missing.
    private const string CheckScript = """
        (elements, failOn) => {
            const tracker = window.__blazorTest;
            if (!tracker) {
                return null;
            }
            const attached = elements.length === 1 && tracker.hasHandlers(elements[0]);
            return JSON.stringify({ attached, failure: tracker.failure(failOn) });
        }
        """;

    /// <summary>Waits until the element exists and Blazor has attached its event handlers.</summary>
    public static async Task WaitForBlazorInteractiveAsync(this ILocator locator, BlazorTestOptions? options = null)
    {
        var resolved = Tracker.OptionsFor(locator.Page, options);
        var failOn = Tracker.FailureKinds(resolved.FailOn);
        var clock = Stopwatch.StartNew();

        // Blazor can replace prerendered elements when it attaches, so the locator is resolved again on every check.
        while (clock.Elapsed < resolved.Timeout)
        {
            var json = await locator.EvaluateAllAsync<string?>(CheckScript, failOn) ?? throw Tracker.NotInstalled();
            var check = JsonSerializer.Deserialize<HandlerCheck>(json, JsonSerializerOptions.Web)!;
            if (check.Failure is not null)
            {
                throw Tracker.Failed(locator.Page, $"{locator} to be interactive", check.Failure.Kind, check.Failure.Message);
            }

            if (check.Attached)
            {
                return;
            }

            await Task.Delay(20);
        }

        var timeout = Tracker.Seconds(resolved.Timeout);
        throw new BlazorTimeoutException(Tracker.WithServerLog(locator.Page,
            $"{locator} was not interactive after {timeout}: it was missing or had no Blazor event handlers."));
    }

    private sealed record HandlerCheck(bool Attached, TrackerFailure? Failure);

    private sealed record TrackerFailure(string Kind, string Message);
}
