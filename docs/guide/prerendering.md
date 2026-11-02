# Prerendered pages

By default, a Blazor Web App prerenders interactive components: the server sends their HTML with the first response,
and Blazor attaches them to the circuit or the WebAssembly runtime a moment later. Until it does, the page looks
finished but nothing on it responds.

Playwright can't tell the difference. Its actionability checks (visible, enabled, stable, receiving events) all pass
for a prerendered button, so `ClickAsync` clicks it at once. The click does nothing, and the test fails later with an
assertion that makes no sense.

## Waiting for handlers

`GotoBlazorAsync` and `WaitForBlazorIdleAsync` wait until every interactive root component on the page is attached and
has its event handlers. Once they return, a click lands.

To wait for one element instead, use the locator wait:

```csharp
var save = Page.GetByRole(AriaRole.Button, new() { Name = "Save" });
await save.WaitForBlazorInteractiveAsync();
await save.ClickAsync();
```

It waits until the locator matches exactly one element and Blazor has attached an event handler to it. Blazor can
replace a prerendered element when it attaches, so the wait finds the element again on every check rather than keeping
the first one it saw.

## Interactive Auto

With Interactive Auto, a component runs on the server the first time and on WebAssembly once the runtime has
downloaded. Either way, the waits count it as interactive when its handlers are attached, so tests don't need to know
which runtime a component ended up on.

## Elements without handlers

`locator.WaitForBlazorInteractiveAsync()` waits for Blazor's own event handlers. An element that has none, such as a
plain link or a static form field, never becomes interactive in this sense and the wait times out. Wait for the page
instead, or for a nearby element that has a Blazor event handler.
