using Microsoft.Extensions.Logging;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>A logger factory that keeps every message, so a test can read what the build logged.</summary>
internal sealed class RecordingLoggerFactory : ILoggerFactory
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines
    {
        get
        {
            lock (_lines)
            {
                return [.. _lines];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }

    private sealed class Logger(RecordingLoggerFactory factory) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (factory._lines)
            {
                factory._lines.Add(formatter(state, exception));
            }
        }
    }
}
