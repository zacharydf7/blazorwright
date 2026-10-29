using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazorwright;

/// <summary>
/// Runs the app in-process on real Kestrel, on a port the OS picks, and keeps its log lines in <see cref="Log"/>. The
/// server starts the first time <see cref="BaseUrl"/> is read.
/// </summary>
public class BlazorAppFixture<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly Lazy<string> _baseUrl;

    public BlazorAppFixture()
    {
        UseKestrel(0);
        _baseUrl = new(Start);
    }

    public string BaseUrl => _baseUrl.Value;

    public ServerLog Log { get; } = new();

    /// <summary>Changes the app for tests: settings, services or anything else on the web host builder.</summary>
    protected virtual void ConfigureApp(IWebHostBuilder app)
    {
    }

    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(Log));
        ConfigureApp(builder);
    }

    private string Start()
    {
        StartServer();
        return Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
    }
}
