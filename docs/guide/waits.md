# The waits

Blazorwright adds four waits to Playwright. Each returns as soon as its condition holds, and throws
`BlazorTimeoutException` if it doesn't hold within the [timeout](options.md), 30 s by default.

| Method | Returns when |
| --- | --- |
| `Page.GotoBlazorAsync(url)` | The page has loaded, its interactive components are attached, and the app is idle |
| `Page.WaitForBlazorIdleAsync()` | No .NET calls are in flight, no render batches are pending, and nothing has changed for the quiet period |
| `Page.WaitForBlazorInteractiveAsync()` | Every interactive root component on the page has its event handlers attached |
| `locator.WaitForBlazorInteractiveAsync()` | That element exists and has its event handlers attached |

## Idle: after the test does something

Call `WaitForBlazorIdleAsync` after any action that makes the app do work: a click, typing into a bound input,
submitting a form, pressing a key. Then read the page.

```csharp
await Page.GetByLabel("Search").FillAsync("blue");
await Page.WaitForBlazorIdleAsync();

Assert.Equal(4, await Page.GetByRole(AriaRole.Listitem).CountAsync());
```

The app is idle when all of these are true:

- No .NET call is in flight. That covers every event handler, `[JSInvokable]` method and call from .NET to JavaScript
  (`IJSRuntime.InvokeAsync`), including the time the handler spends awaiting a database or an HTTP call.
- No render batch is pending: every render the server has sent has been applied to the page.
- No root component is still attaching, and no enhanced navigation is in progress.
- Nothing has changed in the DOM for the quiet period, 100 ms by default.

The quiet period covers short work that nothing awaits, such as a timer or a `_ = LoadAsync();` that finishes quickly.

Idle is also the way to check that something did *not* happen. Once the app is idle, it has finished reacting, so the
absence of a message is real:

```csharp
await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
await Page.WaitForBlazorIdleAsync();

Assert.Equal(0, await Page.GetByRole(AriaRole.Alert).CountAsync());
```

## Opening a page

`GotoBlazorAsync` is `GotoAsync` followed by an idle wait. Idle includes interactivity, so when it returns, every
interactive component on the page has its handlers attached.

```csharp
await Page.GotoBlazorAsync("/orders/42");
```

Use it for every page a test opens. Links and enhanced navigation inside a test need only an idle wait after the click:

```csharp
await Page.GetByRole(AriaRole.Link, new() { Name = "Next" }).ClickAsync();
await Page.WaitForBlazorIdleAsync();
```

## Interactive: before the first click

`WaitForBlazorInteractiveAsync` waits only for event handlers, not for the app to settle. It's for pages that are busy
on purpose, such as one that polls or animates, where idle would wait for the quiet period on every change.

```csharp
await Page.GotoAsync("/dashboard");
await Page.WaitForBlazorInteractiveAsync();
```

The locator version waits for one element. It suits a component that becomes interactive later than the rest of the
page, such as one in a lazy-loaded section:

```csharp
var editor = Page.GetByRole(AriaRole.Textbox, new() { Name = "Notes" });
await editor.WaitForBlazorInteractiveAsync();
await editor.FillAsync("Ship on Monday");
```

[Prerendered pages](prerendering.md) explains why a visible element may not be interactive yet.

## Waiting for something specific

Idle covers what the app awaits. For work it doesn't await, such as a background load started with
`_ = LoadAsync();` that takes longer than the quiet period, wait for its result with Playwright's assertions, which
retry until they pass:

```csharp
await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Import finished");
```

Assertions also suit states the app ends in. Don't check for a short-lived state, such as a loading message, after a wait:
by the time the wait returns, the message may already be gone.

## Passing options to one wait

Each wait takes an optional `BlazorTestOptions`, which replaces the test class's options for that wait. To change one
setting and keep the rest, start from the class's options:

```csharp
await Page.WaitForBlazorIdleAsync(Options with { Timeout = TimeSpan.FromMinutes(2) });
```

See [Options](options.md) for the settings and how to change them for a whole test class.
