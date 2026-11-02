# Roadmap

Blazorwright is working toward its first release. The packages aren't on NuGet yet.

## 1.0

The first stable release aims at one promise: a test that waits for idle sees the app's final state, on every render
mode, without sleeps or retries. The work toward it is tracked in
[GitHub issues](https://github.com/zacharydf7/blazorwright/issues), and falls into four groups:

- **Correctness of idle.** Park a call only for modal dialogs, so a popover doesn't end a wait early
  ([#18](https://github.com/zacharydf7/blazorwright/issues/18)), and close or document the window in which idle can
  return before a JavaScript call made from `OnAfterRenderAsync` starts
  ([#21](https://github.com/zacharydf7/blazorwright/issues/21)).
- **Coverage.** Optionally count elements marked `aria-busy="true"` as busy
  ([#19](https://github.com/zacharydf7/blazorwright/issues/19)).
- **Diagnostics.** Name what kept the page from going quiet when idle times out
  ([#20](https://github.com/zacharydf7/blazorwright/issues/20)), and fail clearly when a Server circuit doesn't use
  WebSockets ([#9](https://github.com/zacharydf7/blazorwright/issues/9)).
- **Confidence.** Run the browser tests against each supported Blazor version
  ([#10](https://github.com/zacharydf7/blazorwright/issues/10)) and on WebKit
  ([#11](https://github.com/zacharydf7/blazorwright/issues/11)), and test apps hosted out of process
  ([#14](https://github.com/zacharydf7/blazorwright/issues/14)).

## Later

- Browser fakes for the save-file picker, the clipboard and downloads
  ([#13](https://github.com/zacharydf7/blazorwright/issues/13)).
- Base classes for MSTest and NUnit ([#15](https://github.com/zacharydf7/blazorwright/issues/15)).
- A fixture that hosts more than one app ([#16](https://github.com/zacharydf7/blazorwright/issues/16)).
- Helpers for updates pushed over SignalR hubs ([#17](https://github.com/zacharydf7/blazorwright/issues/17)).

Ideas and bug reports are welcome as issues.
