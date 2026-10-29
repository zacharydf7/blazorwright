using Microsoft.Playwright;

namespace Blazorwright;

public static class BlazorBrowserContextExtensions
{
    /// <summary>
    /// Installs the Blazor tracker on every page this context opens from now on. Call it before the first page opens;
    /// <paramref name="options"/> become the default for that context's waits.
    /// </summary>
    public static async Task AddBlazorTrackingAsync(this IBrowserContext context, BlazorTestOptions? options = null)
    {
        await context.AddInitScriptAsync(Tracker.Script);
        Tracker.Register(context, options ?? BlazorTestOptions.Default);
    }
}
