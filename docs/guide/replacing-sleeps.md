# From sleeps to waits

This tutorial moves an existing Playwright suite for a Blazor app onto Blazorwright, one pattern at a time. Each step
keeps the suite green, so you can move a few tests over at once.

## 1. Install the tracker

If the suite already has its own base class, keep it and install the tracker on each test's browser context before the
first page opens:

```csharp
await Context.AddBlazorTrackingAsync();
```

If it derives from Playwright's `PageTest` on xUnit v3, switch to `BlazorPageTest` instead; see
[Getting started](getting-started.md). `PageTest` opens its page before a subclass could install the tracker, which is
why `BlazorPageTest` derives from `ContextTest`.

## 2. Replace waits after navigation

Look for navigation followed by a sleep, a wait for the network, or a wait for some element that only shows up once
the app is ready:

```csharp
// Before
await Page.GotoAsync("/orders");
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await Page.WaitForTimeoutAsync(1000);
```

```csharp
// After
await Page.GotoBlazorAsync("/orders");
```

`NetworkIdle` doesn't see Blazor Server's work: the circuit's messages travel over one open WebSocket, not as separate
requests. The sleep covered the time Blazor took to attach handlers, which `GotoBlazorAsync` now waits for exactly.

## 3. Replace sleeps after actions

```csharp
// Before
await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
await Page.WaitForTimeoutAsync(500);
Assert.Equal("Saved", await Page.GetByRole(AriaRole.Status).TextContentAsync());
```

```csharp
// After
await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
await Page.WaitForBlazorIdleAsync();
Assert.Equal("Saved", await Page.GetByRole(AriaRole.Status).TextContentAsync());
```

The idle wait is usually shorter than the sleep it replaces, and it can't return before the handler and its render have
finished.

## 4. Keep assertions that wait for a result

`Expect(...).ToHaveTextAsync(...)` and the other Playwright assertions retry until they pass. They're still right for
results the app produces without awaiting them, such as a background import. After an idle wait they pass on the
first try, so they cost nothing to keep.

What changes is the negative check. A check that something is *absent* passes immediately with or without the app's
help, so it needs an idle wait first:

```csharp
await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
await Page.WaitForBlazorIdleAsync();
await Expect(Page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
```

## 5. Remove retries

Once a test has no sleeps, remove its retry attribute and run it many times. On a test that still fails now and then,
the failure message says what was still running when the wait timed out. See
[Failure messages and artifacts](failures.md).

## 6. Tune the timeout

Waits time out after 30 s by default. A suite with long server work, such as file processing, can raise it for a test
class:

```csharp
protected override BlazorTestOptions Options => new() { Timeout = TimeSpan.FromMinutes(2) };
```

The timeout is a ceiling, not a delay: a wait returns as soon as the app is idle.
