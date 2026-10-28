# Blazorwright

Deterministic Playwright tests for Blazor apps on .NET 10.

`await Page.WaitForBlazorIdleAsync()` tells a test that the app has finished reacting to what the test just did: no
event handler still running, no server render still on its way to the browser. It needs no changes to the app under
test, so retries and fixed sleeps can go.

Status: feasibility spikes. Nothing is published yet.

## License

MIT
