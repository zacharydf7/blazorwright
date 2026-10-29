using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Spike.Tests;

// The sample app in-process on real Kestrel, on a port the OS picks (Q5).
public class SpikeApp : WebApplicationFactory<Program>
{
    public const string RendererCategory = "Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer";

    private readonly Dictionary<string, string> _settings;

    public SpikeApp(Dictionary<string, string>? settings = null)
    {
        _settings = settings ?? [];
        UseKestrel(0);
        StartServer();
        var addresses = Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>();
        BaseUrl = addresses.Addresses.First();
    }

    public ServerLog Log { get; } = new();

    public string BaseUrl { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureLogging(logging => logging.ClearProviders().AddProvider(Log).AddFilter<ServerLog>(RendererCategory, LogLevel.Debug));
    }
}
