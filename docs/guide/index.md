# Introduction

Blazorwright makes Playwright tests of Blazor apps deterministic. After a test clicks, types or navigates,
`await Page.WaitForBlazorIdleAsync()` returns once the app has finished reacting: no event handler still running, no
server render still on its way to the browser, and no change to the page for a short quiet period. It needs no changes
to the app under test.

It also:

- waits for interactivity, so a test doesn't click a prerendered button before Blazor has attached its handler;
- hosts the app in the test process on real Kestrel, so the browser talks to it over a real port;
- stops a wait at once when the app fails, and puts the server's exception in the message;
- keeps a Playwright trace and video for every failed test.

## Why tests of Blazor apps are flaky

Playwright waits for an element to be visible, enabled and stable before it acts on it. That covers most web apps, but
Blazor does much of its work where Playwright can't see it:

- **Prerendering.** The server sends finished HTML first and attaches event handlers later. A button is visible and
  enabled before it does anything, so an early click is lost.
- **Server round trips.** On Blazor Server, a click becomes a message to the server. The handler may await a
  database call, then the server sends a render batch back. Until that batch is applied, the page shows the old state,
  and an assertion that runs now sees it.
- **Things that didn't happen.** A test that checks a validation message did *not* appear has nothing to wait for.
  Without a signal, the only option is to sleep and hope.

Suites end up with `Task.Delay` calls, retries and generous timeouts. They get slower, and they still fail now and
then.

## What Blazorwright does instead

Blazorwright installs a small script in every page the test opens. The script watches Blazor from the inside: the
.NET calls in flight, the render batches the server has sent and the browser hasn't applied, the components still
attaching, enhanced navigations, and the DOM. A wait returns when all of them are done, so the test continues as soon
as the app is ready and no sooner.

```csharp
await Page.GotoBlazorAsync("/orders");

await Page.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();
await Page.WaitForBlazorIdleAsync();

Assert.Equal(3, await Page.GetByRole(AriaRole.Row).CountAsync() - 1);
```

[How it works](how-it-works.md) describes what the script watches. [Getting started](getting-started.md) sets up a test
project from scratch.
