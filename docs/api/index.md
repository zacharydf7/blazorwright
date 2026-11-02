# API reference

Blazorwright has two packages:

- **Blazorwright**: the waits, as extension methods on Playwright's `IPage`, `ILocator` and `IBrowserContext`, plus
  `BlazorAppFixture`, which hosts the app for the tests. Use it with any test framework.
- **Blazorwright.Xunit**: `BlazorPageTest`, the xUnit v3 base class that gives each test a tracked page and keeps a
  trace and video when it fails.

Start with [`BlazorPageExtensions`](Blazorwright.BlazorPageExtensions.yml) for the waits, or with
[`BlazorPageTest`](Blazorwright.Xunit.BlazorPageTest.yml) for the base class.
