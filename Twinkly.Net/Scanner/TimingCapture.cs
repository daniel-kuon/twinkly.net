using System;
using System.Collections.Generic;
using System.Linq;

namespace Scanner;

public class TimingCapture
{
    private readonly int _sampleSize;
    private readonly Dictionary<string, List<int>> _timings = new();
    private Dictionary<string, int> _subCaptureTimings = new();

    public TimingCapture(int sampleSize = 1, TimingCapture? subCapture = null)
    {
        _sampleSize = sampleSize;
        if (subCapture is not null) subCapture.OnTimingsUpdated += (sender, timings) => _subCaptureTimings = timings;

        OnTimingsUpdated += (sender, timings) =>
        {
            var timingsString =
                string.Join(Environment.NewLine,
                    timings.Union(_subCaptureTimings).Select(t => $"{t.Key}: {t.Value}ms"));
            OnTimingStringUpdated?.Invoke(this, timingsString);
        };
    }

    public Watch CreateWatch(string name)
    {
        return new Watch(elapsed => AddTiming(name, elapsed));
    }

    private void AddTiming(string name, TimeSpan elapsed)
    {
        if (!_timings.ContainsKey(name)) _timings.Add(name, new List<int>());

        _timings[name].Add((int)elapsed.TotalMilliseconds);
        if (_timings[name].Count > _sampleSize) _timings[name].RemoveAt(0);

        OnTimingsUpdated?.Invoke(this, _timings.ToDictionary(t => t.Key, t => (int)t.Value.Average()));
    }


    public event EventHandler<Dictionary<string, int>>? OnTimingsUpdated;
    public event EventHandler<string>? OnTimingStringUpdated;
}