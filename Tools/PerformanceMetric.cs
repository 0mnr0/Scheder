using System.Diagnostics;
namespace Scheder.Tools;

public enum MetricType {
    Total,
    Analyze, // Context Detection and day detection
    Build,
    Draft,
    TokenParse,
    DataParse,
    MessageSend,
    WeatherFetch,
    WeatherRender,
}

public class PerformanceMetric {
    private readonly Dictionary<MetricType, Stopwatch> _watches = new();

    public PerformanceMetric() {
        foreach (var type in Enum.GetValues<MetricType>())
            _watches[type] = new Stopwatch();
    }

    public IDisposable Measure(MetricType type) {
        var watch = Start(type);
        return new StopOnDispose(watch);
    }

    public Stopwatch Start(MetricType type) {
        var watch = _watches[type];
        watch.Start();
        return watch;
    }

    public Stopwatch Stop(MetricType type) {
        var watch = _watches[type];
        watch.Stop();
        return watch;
    }

    public void StopAll() {
        foreach (var watch in _watches) {
            watch.Value.Stop();
        }
    }

    public long GetMetric(MetricType type, bool asNanoSeconds = false) {
        var watch = _watches[type];
        return asNanoSeconds ? watch.ElapsedTicks * 1000000 / Stopwatch.Frequency : watch.ElapsedMilliseconds;
    }

    public string GetExactMetric(MetricType type, bool asNanoSeconds = false) {
        var watch = _watches[type];
        var nsTime = asNanoSeconds ? watch.ElapsedTicks * 1000000 / Stopwatch.Frequency : watch.ElapsedMilliseconds;
        if (asNanoSeconds && nsTime > 1000 || !asNanoSeconds) {
            return $"{watch.ElapsedMilliseconds} мс";
        }

        return $"{nsTime} мКс";
    }

    public TimeSpan Elapsed(MetricType type) => _watches[type].Elapsed;

    private sealed class StopOnDispose(Stopwatch watch) : IDisposable {
        public void Dispose() => watch.Stop();
    }
}

public interface IMetricHandle {
    void Start();
    void Stop();
}