using Microsoft.Playwright;

namespace Blazorwright;

public static class BlazorPageExtensions
{
    /// <summary>Opens <paramref name="url"/> and waits until its interactive components are attached and the app is idle.</summary>
    public static async Task<IResponse?> GotoBlazorAsync(this IPage page, string url, BlazorTestOptions? options = null)
    {
        var response = await page.GotoAsync(url);
        await page.WaitForBlazorIdleAsync(options);
        return response;
    }

    /// <summary>
    /// Waits until no .NET calls are in flight, no render batches are pending and nothing has changed for the quiet
    /// period. Work that no handler awaits is covered only by the quiet period.
    /// </summary>
    public static Task WaitForBlazorIdleAsync(this IPage page, BlazorTestOptions? options = null) =>
        Tracker.WaitAsync(page, Tracker.Idle, options);

    /// <summary>Waits until every interactive root component on the page has its event handlers attached.</summary>
    public static Task WaitForBlazorInteractiveAsync(this IPage page, BlazorTestOptions? options = null) =>
        Tracker.WaitAsync(page, Tracker.Interactive, options);
}
