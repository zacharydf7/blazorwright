# Dialogs

Many apps open a dialog from an event handler and await the user's answer:

```csharp
private async Task RenameAsync()
{
    var name = await _renameDialog.ShowAsync(); // waits for the user
    if (name is not null)
    {
        await Files.RenameAsync(_file, name);
    }
}
```

The handler is a .NET call that stays in flight until the dialog closes. Counted like any other call, it would keep the
app busy, and a test that clicks Rename and waits for idle would time out waiting for itself to fill in the dialog.

## Parked calls

Blazorwright notes which dialogs are open when each .NET call starts. If a new dialog opens while the call runs, the
call is *parked*: it stops counting while that dialog is visible, and counts again once the dialog closes. So a test
can open a dialog, wait for idle, fill it in, and wait for idle again:

```csharp
await Page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
await Page.WaitForBlazorIdleAsync();

await Page.GetByLabel("Name", new() { Exact = true }).FillAsync("report-final.pdf");
await Page.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
await Page.WaitForBlazorIdleAsync(); // waits for RenameAsync to finish

await Expect(Page.GetByText("report-final.pdf")).ToBeVisibleAsync();
```

A dialog is any visible element that matches one of these:

- `aria-modal="true"`
- `role="dialog"` or `role="alertdialog"`
- a `<dialog>` opened with `showModal()`

Most component libraries mark their dialogs this way already, because screen readers need it too.

## A timeout that names a parked call

If idle times out while a call is parked, the message lists it under "waiting on a dialog":

```
Not idle after 30 s: no calls in flight, 1 call waiting on a dialog (DispatchEventAsync), 1 render batch pending,
last activity 29870 ms ago.
```

A parked call doesn't keep the app busy, so it isn't the cause of the timeout; look at the other parts of the message.

## Popovers

A non-modal popover that sets `role="dialog"`, such as a date picker's panel, parks the call that opened it too. If that
handler keeps working after the panel shows, idle can return before it finishes. Wait for the result with a Playwright
assertion in that case. [Issue #18](https://github.com/zacharydf7/blazorwright/issues/18) tracks parking calls only for
modal dialogs.
