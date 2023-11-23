using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Scanner;

public class Watch : IDisposable
{
    private readonly Action<TimeSpan> _onCompleted;
    private readonly Stopwatch _stopwatch = new();

    public Watch(Action<TimeSpan> onCompleted)
    {
        _onCompleted = onCompleted;
        _stopwatch.Start();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _onCompleted(_stopwatch.Elapsed);
    }
}
public class StringLogger<T> : StringLogger, ILogger<T>
{
    public StringLogger(LogLevel level = LogLevel.Debug) : base(level)
    {
    }
}


public class StringLogger : ILogger
{
    public StringLogger(LogLevel level = LogLevel.Debug)
    {
        Level = level;
    }

    public EventHandler<string>? OnWrite { get; set; }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel < Level) return;
        OnWrite?.Invoke(this, $"[{logLevel}] {formatter(state, exception)}");
    }

    public LogLevel Level { get; set; }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= Level;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}
