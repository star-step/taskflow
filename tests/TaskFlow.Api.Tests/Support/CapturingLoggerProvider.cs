using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Api.Tests.Support;

/// <summary>Records every log entry (message and structured state) so tests can assert on logging.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    public sealed record LogEntry(string Category, LogLevel Level, string Message, IReadOnlyDictionary<string, object?> State);

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state as IEnumerable<KeyValuePair<string, object?>> ?? Array.Empty<KeyValuePair<string, object?>>();
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception),
                values.ToDictionary(kv => kv.Key, kv => kv.Value)));
        }
    }
}
