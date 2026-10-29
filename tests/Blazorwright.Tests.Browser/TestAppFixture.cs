using Microsoft.AspNetCore.Hosting;

namespace Blazorwright.Tests.Browser;

public class TestAppFixture : BlazorAppFixture<Program>
{
    protected virtual int NegotiateDelayMs => 0;

    protected override void ConfigureApp(IWebHostBuilder app) => app.UseSetting("NegotiateDelayMs", NegotiateDelayMs.ToString());
}

// Circuits start 300 ms late, so a click straight after GotoAsync can beat the handlers, as on a busy server.
public class SlowCircuitAppFixture : TestAppFixture
{
    protected override int NegotiateDelayMs => 300;
}
