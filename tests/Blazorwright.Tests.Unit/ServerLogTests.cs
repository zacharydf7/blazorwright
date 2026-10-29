using Microsoft.Extensions.Logging;

namespace Blazorwright.Tests.Unit;

public class ServerLogTests
{
    [Fact]
    public void Since_ShouldReturnOnlyLaterLines_WhenGivenEarlierPosition()
    {
        var log = new ServerLog();
        var logger = log.CreateLogger("App");
        logger.LogWarning("Before");
        var position = log.Position;

        logger.LogError("After");

        Assert.Equal(["After"], log.Since(position).Select(entry => entry.Message));
    }

    [Fact]
    public void ToString_ShouldIncludeException_WhenLineHasOne()
    {
        var log = new ServerLog();
        log.CreateLogger("App.Circuit").LogError(new InvalidOperationException("Boom"), "Unhandled exception");

        var line = log.Since(0).Single().ToString();

        Assert.StartsWith($"Error App.Circuit: Unhandled exception{Environment.NewLine}System.InvalidOperationException: Boom", line);
    }
}
