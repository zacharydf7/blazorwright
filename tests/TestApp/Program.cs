using TestApp;
using TestApp.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddSingleton<Ticker>();
builder.Services.AddHostedService(services => services.GetRequiredService<Ticker>());

var app = builder.Build();

// Lets a test slow down circuit start, the way a busy server does.
var negotiateDelayMs = app.Configuration.GetValue<int>("NegotiateDelayMs");
app.Use(async (context, next) =>
{
    if (negotiateDelayMs > 0 && context.Request.Path.StartsWithSegments("/_blazor/negotiate"))
    {
        await Task.Delay(negotiateDelayMs);
    }

    await next();
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TestApp.Client.Counter).Assembly);
app.Run();

public partial class Program;
