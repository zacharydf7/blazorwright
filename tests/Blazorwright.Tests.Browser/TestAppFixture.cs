using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Blazorwright.Tests.Browser;

// The sample app in-process on real Kestrel, on a port the OS picks.
public class TestAppFixture : WebApplicationFactory<Program>
{
    public TestAppFixture()
    {
        UseKestrel(0);
        StartServer();
        BaseUrl = Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
    }

    public string BaseUrl { get; }

    protected virtual int NegotiateDelayMs => 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("NegotiateDelayMs", NegotiateDelayMs.ToString());
}

// Circuits start 300 ms late, so a click straight after GotoAsync can beat the handlers, as on a busy server.
public class SlowCircuitAppFixture : TestAppFixture
{
    protected override int NegotiateDelayMs => 300;
}
