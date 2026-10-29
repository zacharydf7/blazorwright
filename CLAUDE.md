# Blazorwright

## Clean-room rule

The idea for this package came from a product at the maintainer's employer. Nothing from that product or its company
ever enters this repo:

- No code from it, and no logic ported or adapted from it.
- None of its type, file, project, database or environment-variable names, and none of its test data, scripts or ticket
  keys.
- No mention of the product, the company or its other repos. Their names are in `CLAUDE.local.md`, which is not
  committed.
- This covers code, tests, `spike/`, README, CLAUDE.md, commit messages, issues and package metadata.
- Write from public sources only: ASP.NET Core and Playwright source and docs, protocol specs. Spikes and tests use
  sample apps invented here, with neutral names.
- Cross-checks against the employer's product run outside this repo, and their results are recorded only in the
  private design spec.

## What this is

Blazorwright makes Playwright tests of Blazor apps deterministic: `await Page.WaitForBlazorIdleAsync()` tells a test
the app has finished reacting, with no changes to the app under test. It also checks interactivity after prerender,
hosts the app in-process on real Kestrel, and keeps failure artifacts. Two packages are planned: `Blazorwright` and
`Blazorwright.Xunit`.

The project is in its feasibility phase. `spike/` holds throwaway code that answers the design spec's feasibility
questions; package code goes in `src/` and `tests/` once the go/no-go gate passes.

## Build and test

`Blazorwright.slnx` is empty until package code exists. The spike has its own solution; CI only builds it.
`SPIKE_RUNS` and `SPIKE_SWEEP_RUNS` set the run counts (default 10); results land in
`spike/Spike.Tests/bin/Release/net10.0/spike-results/`.

```
dotnet build spike/Spike.slnx -c Release
pwsh spike/Spike.Tests/bin/Release/net10.0/playwright.ps1 install chromium firefox
dotnet test --solution spike/Spike.slnx -c Release
```

- Tests run on Microsoft.Testing.Platform (xUnit v3): filter with `--filter-class` and `--filter-method`, not VSTest's
  `--filter`.
- Warnings are errors.
- CI builds every push and pull request on Windows and Linux. Pushing a `v*` tag publishes the packages to nuget.org;
  only the maintainer creates tags.

## Tests

- Name tests `Method_Should<Expectation>_When<Condition>`. Separate arrange, act and assert with blank lines, not
  comments.

## Code style

- Write for an average developer: small focused classes, familiar idioms, and at most a one-line comment explaining
  why.
- Make every option discoverable through an enum or static class.
- The tracker script is plain JavaScript with no npm build step.
- Use American English. Fill lines to about 120 characters instead of putting one parameter per line.
