using Microsoft.Extensions.Logging;

namespace Blazorwright;

public record ServerLogEntry(DateTimeOffset Time, LogLevel Level, string Category, string Message, Exception? Exception)
{
    public override string ToString() =>
        $"{Level} {Category}: {Message}" + (Exception is null ? "" : $"{Environment.NewLine}{Exception}");
}

/// <summary>Keeps the app's log lines in memory, so a failed wait can show what the server logged during the test.</summary>
public sealed class ServerLog : ILoggerProvider
{
    private readonly List<ServerLogEntry> _entries = [];
    private readonly Lock _lock = new();

    /// <summary>The number of lines so far. Pass it to <see cref="Since"/> to get the lines logged after it.</summary>
    public int Position
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }

    public IReadOnlyList<ServerLogEntry> Since(int position)
    {
        lock (_lock)
        {
            return _entries[Math.Min(position, _entries.Count)..];
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose()
    {
    }

    private void Add(ServerLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
    }

    private sealed class Logger(ServerLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            log.Add(new ServerLogEntry(DateTimeOffset.Now, logLevel, category, formatter(state, exception), exception));
    }
}
