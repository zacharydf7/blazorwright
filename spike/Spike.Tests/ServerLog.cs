using Microsoft.Extensions.Logging;

namespace Spike.Tests;

public record ServerLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

// Keeps the server's log lines in memory so a test can read the ones written while it ran.
public class ServerLog : ILoggerProvider
{
    private readonly List<ServerLogEntry> _entries = [];

    public int Position
    {
        get
        {
            lock (_entries)
            {
                return _entries.Count;
            }
        }
    }

    public List<ServerLogEntry> Since(int position)
    {
        lock (_entries)
        {
            return _entries[position..];
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose()
    {
    }

    private void Add(ServerLogEntry entry)
    {
        lock (_entries)
        {
            _entries.Add(entry);
        }
    }

    private class Logger(ServerLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            log.Add(new ServerLogEntry(category, logLevel, formatter(state, exception), exception));
        }
    }
}
