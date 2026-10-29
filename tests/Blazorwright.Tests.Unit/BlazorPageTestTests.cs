using Blazorwright.Xunit;

namespace Blazorwright.Tests.Unit;

public class BlazorPageTestTests
{
    [Fact]
    public void FolderName_ShouldKeepName_WhenNameIsShortAndSafe()
    {
        var folder = BlazorPageTest.FolderName("CounterTests.Counter_ShouldIncrement_WhenClicked");

        Assert.Equal("CounterTests.Counter_ShouldIncrement_WhenClicked", folder);
    }

    [Fact]
    public void FolderName_ShouldGiveTheoryRowsDifferentFolders_WhenNamesDifferOnlyPastTheCut()
    {
        var name = "Blazorwright.Tests.Browser.WaitForBlazorIdleTests.WaitForBlazorIdleAsync_ShouldReturnAfterAwaitedWork_WhenHandlerAwaitsDelay";

        var chromium = BlazorPageTest.FolderName($"{name}(browser: \"chromium\", mode: \"server\")");
        var firefox = BlazorPageTest.FolderName($"{name}(browser: \"firefox\", mode: \"server\")");

        Assert.NotEqual(chromium, firefox);
        Assert.All([chromium, firefox], folder => Assert.Equal(100, folder.Length));
    }

    [Fact]
    public void FolderName_ShouldReplaceSlash_WhenNameContainsOne()
    {
        var folder = BlazorPageTest.FolderName("PathTests.Open_ShouldWork_WhenGivenPath(path: \"a/b\")");

        Assert.DoesNotContain('/', folder);
    }
}
