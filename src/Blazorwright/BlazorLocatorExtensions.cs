using System.Diagnostics;
using Microsoft.Playwright;

namespace Blazorwright;

public static class BlazorLocatorExtensions
{
    /// <summary>Waits until the element exists and Blazor has attached its event handlers.</summary>
    public static async Task WaitForBlazorInteractiveAsync(this ILocator locator, BlazorTestOptions? options = null)
    {
        var timeout = Tracker.OptionsFor(locator.Page, options).Timeout;
        var clock = Stopwatch.StartNew();

        // Blazor can replace prerendered elements when it attaches, so the locator is resolved again on every check.
        while (clock.Elapsed < timeout)
        {
            var timeLeft = (float)(timeout - clock.Elapsed).TotalMilliseconds;
            var attached = await locator.EvaluateAsync<bool?>("element => window.__blazorTest?.hasHandlers(element) ?? null",
                null, new() { Timeout = Math.Max(1, timeLeft) });
            if (attached is null)
            {
                throw Tracker.NotInstalled();
            }

            if (attached == true)
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new BlazorTimeoutException($"{locator} had no Blazor event handlers after {Tracker.Seconds(timeout)}.");
    }
}
