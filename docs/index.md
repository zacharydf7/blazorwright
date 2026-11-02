---
_layout: landing
title: Blazorwright
description: Deterministic Playwright tests for Blazor apps. WaitForBlazorIdleAsync returns once the app has finished reacting, with no changes to the app under test.
---

<div class="bw-home">

<section class="bw-hero">
<div class="bw-wrap bw-wrap-wide bw-hero-grid">
<div>
<img class="bw-hero-logo" src="assets/logo-dark.svg" alt="Blazorwright">
<h1>Playwright tests for Blazor <span>that wait for the app, not the clock</span></h1>
<p class="bw-lede">Blazorwright tells a test when a Blazor app has finished reacting: no event handler still running,
no server render still on its way to the browser. Swap sleeps and retries for waits that work on Server, WebAssembly
and Auto, with no changes to the app.</p>
<div class="bw-actions">
<a class="bw-btn bw-btn-primary" href="guide/getting-started.md">Get started <i class="bi bi-arrow-right"></i></a>
<a class="bw-btn bw-btn-secondary" href="guide/index.md">Read the guide</a>
<a class="bw-btn bw-btn-secondary" href="https://github.com/zacharydf7/blazorwright"><i class="bi bi-github"></i> GitHub</a>
</div>
<div class="bw-install"><span>$</span> dotnet add package Blazorwright.Xunit</div>
</div>
<div class="bw-window">
<div class="bw-window-bar"><i></i><i></i><i></i><span>CounterTests.cs</span></div>

<pre><code class="lang-csharp">public class CounterTests(AppFixture app) : BlazorPageTest(app)
{
    [Fact]
    public async Task Counter_ShouldIncrement_WhenClicked()
    {
        // Waits until Blazor has attached the button's handler.
        await Page.GotoBlazorAsync("/counter");

        await Page.GetByText("Click me").ClickAsync();

        // Waits for the handler and its render, then returns.
        await Page.WaitForBlazorIdleAsync();

        var status = Page.GetByRole(AriaRole.Status);
        Assert.Equal("Current count: 1", await status.TextContentAsync());
    }
}</code></pre>

</div>
</div>
</section>

<section class="bw-why">
<div class="bw-wrap">
<div class="bw-section-head">
<h2>Why Blazor tests flake</h2>
<p>Playwright waits for elements, but Blazor does its work where Playwright can't see it.</p>
</div>
<div class="bw-steps">
<div class="bw-step">
<span class="bw-step-num">1</span>
<h3>The button isn't ready</h3>
<p>A prerendered button is visible before Blazor attaches its click handler, so an early click is lost.</p>
</div>
<div class="bw-step">
<span class="bw-step-num">2</span>
<h3>The render is in transit</h3>
<p>After a click, the handler may still be awaiting the server, and its render may still be on the way. The assertion
sees the old page.</p>
</div>
<div class="bw-step">
<span class="bw-step-num">3</span>
<h3>Nothing to wait for</h3>
<p>A test that checks something did <em>not</em> happen has no element to wait for, so it sleeps and hopes.</p>
</div>
</div>
</div>
</section>

<section>
<div class="bw-wrap">
<div class="bw-section-head">
<h2>What Blazorwright does</h2>
<p>A small script inside the page watches Blazor's .NET calls, render batches, components and the DOM. The waits
return the moment the work is done and the page goes quiet.</p>
</div>
<div class="bw-grid">
<a class="bw-card" href="guide/waits.md">
<h3><i class="bi bi-activity"></i>Idle that means idle</h3>
<p>Waits for every event handler, every render batch, every call from .NET to JavaScript and every enhanced
navigation, then for the page to go quiet.</p>
</a>
<a class="bw-card" href="guide/prerendering.md">
<h3><i class="bi bi-hand-index"></i>No lost clicks</h3>
<p>Waits until prerendered components have their event handlers attached, for the whole page or for one element.</p>
</a>
<a class="bw-card" href="guide/support.md">
<h3><i class="bi bi-layers"></i>Every render mode</h3>
<p>Interactive Server, WebAssembly and Auto, prerendered or not, and static pages with enhanced navigation.</p>
</a>
<a class="bw-card" href="guide/hosting.md">
<h3><i class="bi bi-hdd-rack"></i>The app on real Kestrel</h3>
<p>Hosts the app in the test process on a real port, with settings and services you can change for the tests.</p>
</a>
<a class="bw-card" href="guide/failures.md">
<h3><i class="bi bi-lightning-charge"></i>Fails fast, says why</h3>
<p>A timeout names what was still running. A server exception stops the wait at once, with the stack trace in the
message.</p>
</a>
<a class="bw-card" href="guide/failures.md#traces-and-videos">
<h3><i class="bi bi-camera-video"></i>Trace and video on failure</h3>
<p>Every failed test keeps a Playwright trace and a video. Passing tests keep nothing.</p>
</a>
</div>
</div>
</section>

<section class="bw-compare-section">
<div class="bw-wrap">
<div class="bw-section-head">
<h2>Delete the sleeps</h2>
<p>A fixed delay is too long on a fast machine and too short on a busy CI agent. An idle wait is exactly as long as the
app takes.</p>
</div>
<div class="bw-compare">
<div class="bw-window bw-window-before">
<div class="bw-window-bar"><i></i><i></i><i></i><span>Before</span></div>

<pre><code class="lang-csharp">await Page.GotoAsync("/orders");
await Page.WaitForTimeoutAsync(2000);

await Page.GetByText("Save").ClickAsync();
await Page.WaitForTimeoutAsync(1000);

// Passes on a fast machine. Usually.
await Expect(Page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);</code></pre>

</div>
<div class="bw-window bw-window-after">
<div class="bw-window-bar"><i></i><i></i><i></i><span>After</span></div>

<pre><code class="lang-csharp">await Page.GotoBlazorAsync("/orders");

await Page.GetByText("Save").ClickAsync();
await Page.WaitForBlazorIdleAsync();

// The app has finished reacting, so no alert means no alert.
await Expect(Page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);</code></pre>

</div>
</div>
<p class="bw-compare-link"><a href="guide/replacing-sleeps.md">Move an existing suite over <i class="bi bi-arrow-right"></i></a></p>
</div>
</section>

<section class="bw-paths">
<div class="bw-wrap">
<div class="bw-section-head">
<h2>Find your way</h2>
<p>The guide is grouped by what you're trying to do.</p>
</div>
<div class="bw-grid">
<div class="bw-path">
<h3>Get started</h3>
<ul>
<li><a href="guide/index.md">Introduction</a></li>
<li><a href="guide/getting-started.md">Getting started</a></li>
<li><a href="guide/replacing-sleeps.md">From sleeps to waits</a></li>
</ul>
</div>
<div class="bw-path">
<h3>Writing tests</h3>
<ul>
<li><a href="guide/waits.md">The waits</a></li>
<li><a href="guide/prerendering.md">Prerendered pages</a></li>
<li><a href="guide/dialogs.md">Dialogs</a></li>
</ul>
</div>
<div class="bw-path">
<h3>Hosting and frameworks</h3>
<ul>
<li><a href="guide/hosting.md">Hosting the app</a></li>
<li><a href="guide/other-frameworks.md">NUnit, MSTest and plain Playwright</a></li>
</ul>
</div>
<div class="bw-path">
<h3>When a test fails</h3>
<ul>
<li><a href="guide/failures.md">Failure messages and artifacts</a></li>
<li><a href="guide/troubleshooting.md">Troubleshooting</a></li>
</ul>
</div>
<div class="bw-path">
<h3>Under the hood</h3>
<ul>
<li><a href="guide/how-it-works.md">How it works</a></li>
<li><a href="guide/support.md">Support and limits</a></li>
</ul>
</div>
<div class="bw-path">
<h3>Reference</h3>
<ul>
<li><a href="api/index.md">API reference</a></li>
<li><a href="guide/options.md">Options</a></li>
<li><a href="guide/roadmap.md">Roadmap</a></li>
</ul>
</div>
</div>
</div>
</section>

</div>
