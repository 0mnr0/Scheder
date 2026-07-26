namespace Scheder.JournalAPI;

public class CachedScheduleLibrary
{
    public class CacheEntry
    {
        public string? SchedOnDate { get; set; } 
        public string? Sched { get; set; }
        public string? Exams { get; set; }
        public DateTime Update { get; set; }
    }
    
    public static readonly Dictionary<long, Dictionary<string, CacheEntry>> Cache = new();
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(20);
    private static readonly Lock Lock = new();
    private static Thread? _cleanupThread;
    private static volatile bool _cleanupRunning;


    
    public static bool DateExists(long uid, string targetDate)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(uid, out var dates) &&
                dates.TryGetValue(targetDate, out var entry))
            {
                if (DateTime.Now - entry.Update < CacheTime)
                    return true;


                // too old. Delete
                dates.Remove(targetDate);
                if (dates.Count == 0)
                    Cache.Remove(uid);
            }
            return false;
        }
    }

    /// <summary>
    /// Возвращает оставшееся время жизни записи в кэше, либо null,
    /// если записи нет или она уже устарела.
    /// </summary>
    public static TimeSpan? GetFreshness(long uid, string targetDate)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(uid, out var dates) &&
                dates.TryGetValue(targetDate, out var entry))
            {
                var age = DateTime.Now - entry.Update;
                if (age < CacheTime)
                    return CacheTime - age;
            }
            return null;
        }
    }
    
    
    public static bool Add(long uid, string targetDate, string? schedValue, string? examsValue, bool allowReplace = true)
    {
        lock (Lock)
        {
            if (!Cache.TryGetValue(uid, out var dates))
            {
                dates = new Dictionary<string, CacheEntry>();
                Cache[uid] = dates;
            }

            var exists = dates.ContainsKey(targetDate);
            if (exists && !allowReplace)
                return false;

            dates[targetDate] = new CacheEntry
            {
                SchedOnDate = targetDate,
                Sched = schedValue,
                Exams = examsValue,
                Update = DateTime.Now
            };

            return exists;
        }
    }
    
    public static void Delete(long uid, string targetDate)
    {
        lock (Lock)
        {
            if (!Cache.TryGetValue(uid, out var dates)) return;
            dates.Remove(targetDate);
            if (dates.Count == 0)
                Cache.Remove(uid);
        }
    }
    
    public static (string?, string?) GetText(long uid, string targetDate)
    {
        lock (Lock)
        {
            return DateExists(uid, targetDate) ? (Cache[uid][targetDate].Sched, Cache[uid][targetDate].Exams) : (null, null);
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    // Service Cleanup
    public static void StartCleanupThread(TimeSpan? checkInterval = null)
    {
        if (_cleanupRunning)
            return;

        var interval = checkInterval ?? TimeSpan.FromMinutes(5);
        var staleThreshold = TimeSpan.FromTicks(CacheTime.Ticks * 2);

        _cleanupRunning = true;
        _cleanupThread = new Thread(() =>
        {
            while (_cleanupRunning)
            {
                Thread.Sleep(interval);
                CleanupExpired(staleThreshold);
            }
        })
        {
            IsBackground = true,
            Name = "ScheduleCacheCleanup"
        };

        _cleanupThread.Start();
    }
    
    
    public static void StopCleanupThread()
    {
        _cleanupRunning = false;
    }

    private static void CleanupExpired(TimeSpan staleThreshold)
    {
        lock (Lock)
        {
            var now = DateTime.Now;
            var emptyUids = new List<long>();

            foreach (var (key, dates) in Cache)
            {
                var staleDates = new List<string>();

                foreach (var dateEntry in dates)
                {
                    if (now - dateEntry.Value.Update >= staleThreshold)
                        staleDates.Add(dateEntry.Key);
                }

                foreach (var date in staleDates)
                    dates.Remove(date);

                if (dates.Count == 0)
                    emptyUids.Add(key);
            }

            foreach (var uid in emptyUids)
                Cache.Remove(uid);
        }
    }


}