# Hosting the app

`BlazorAppFixture<TProgram>` runs the app in the test process on real Kestrel, on a port the operating system picks.
The browser talks to it over HTTP and WebSockets like it would in production, and the tests can change its settings and
services.

It derives from ASP.NET Core's `WebApplicationFactory<TProgram>`, so everything in
[Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests) applies.

## One app for the test assembly

Start the app once and share it between all tests with an xUnit v3 assembly fixture:

```csharp
[assembly: AssemblyFixture(typeof(AppFixture))]

public class AppFixture : BlazorAppFixture<Program>;
```

Each test still gets its own browser context, with its own cookies and storage, so tests don't see each other's
sessions. The server starts the first time a test reads `BaseUrl`, which `BlazorPageTest` does when it opens the
browser context.

## Changing settings and services

Override `ConfigureApp` to change the app for the tests:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public class AppFixture : BlazorAppFixture<Program>
{
    protected override void ConfigureApp(IWebHostBuilder app)
    {
        app.UseEnvironment("Testing");
        app.UseSetting("ConnectionStrings:Orders", "Data Source=orders-tests.db");
        app.ConfigureTestServices(services => services.AddSingleton<IClock>(new FixedClock(2026, 1, 1)));
    }
}
```

`ConfigureTestServices` runs after the app's own registrations, so it replaces them.

## Two configurations

To test the app with two configurations, such as with a feature flag on and off, declare two fixtures and use each in
its own test classes:

```csharp
public class AppWithBetaFixture : BlazorAppFixture<Program>
{
    protected override void ConfigureApp(IWebHostBuilder app) => app.UseSetting("Features:Beta", "true");
}

public class BetaTests(AppWithBetaFixture app) : BlazorPageTest(app)
{
    // ...
}
```

Register both as assembly fixtures. Each starts its own server, on its own port.

## The server log

The fixture keeps every line the app logs in `Log`. When a wait fails, the message ends with the warnings and errors
logged since the test's browser context opened, so an exception on the server shows up next to the test that caused
it. Tests that share the app and run at the same time add their lines too.

## An app that runs elsewhere

`BlazorPageTest` takes any `IBlazorApp`, so it can also test an app that is already running, such as one started by
the CI pipeline:

```csharp
public class RunningApp : IBlazorApp
{
    public string BaseUrl => Environment.GetEnvironmentVariable("APP_URL") ?? "https://localhost:5001";

    public ServerLog? Log => null;
}
```

Without a log, failure messages can't include the server's errors. Out-of-process apps aren't covered by Blazorwright's
own tests yet; [issue #14](https://github.com/zacharydf7/blazorwright/issues/14) tracks that.
