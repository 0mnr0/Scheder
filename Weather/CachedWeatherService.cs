using Scheder;
using Scheder.Config;
using Telegram.Bot.Types;

public abstract class CachedWeatherService
{
    public class WeatherCacheEntry
    {
        public List<WeatherAPI.WeatherObject> Parsed { get; set; }
        public DateTime Update { get; init; }
        public List<byte[]>? RichImages { get; set; }
    }
    //                                 CITY               DATE      
    public static readonly Dictionary<string, Dictionary<string, WeatherCacheEntry>> Cache = new();
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(Behaviour.Other.WeatherCachingTime); // 20 minutes default
    private static readonly Lock Lock = new();
    private static Thread? _cleanupThread;
    private static volatile bool _cleanupRunning;


    
    public static bool DateExists(string targetCity, string targetDate)
    {
        lock (Lock)
        {
            if (!Behaviour.Other.AllowWeatherCaching) {return false;}
            
            if (Cache.TryGetValue(targetCity, out var dates) &&
                dates.TryGetValue(targetDate, out var entry))
            {
                if (DateTime.Now - entry.Update < CacheTime)
                    return true;
                
                // too old. Delete
                dates.Remove(targetDate);
                if (dates.Count == 0)
                    Cache.Remove(targetCity);
            }
            return false;
        }
    }
    
    public static TimeSpan? GetFreshness(string targetCity, string targetDate)
    {
        lock (Lock)
        {
            if (!Cache.TryGetValue(targetCity, out var dates) ||
                !dates.TryGetValue(targetDate, out var entry)) return null;
            
            var age = DateTime.Now - entry.Update;
            if (age < CacheTime)
                return CacheTime - age;
            return null;
        }
    }
    
    
    public static bool Add(string targetCity, string targetDate, List<WeatherAPI.WeatherObject> weatherData, bool allowReplace = true)
    {
        lock (Lock)
        {
            if (!Behaviour.Other.AllowWeatherCaching) {return false;}
            
            if (!Cache.TryGetValue(targetCity, out var dates))
            {
                dates = new Dictionary<string, WeatherCacheEntry>();
                Cache[targetCity] = dates;
            }

            var exists = dates.ContainsKey(targetDate);
            if (exists && !allowReplace)
                return false;

            dates[targetDate] = new WeatherCacheEntry
            {
                Parsed = weatherData,
                Update = DateTime.Now
            };

            return exists;
        }
    }
    
    public static void Delete(string targetCity, string targetDate)
    {
        lock (Lock)
        {
            if (!Cache.TryGetValue(targetCity, out var dates)) return;
            dates.Remove(targetDate);
            if (dates.Count == 0)
                Cache.Remove(targetCity);
        }
    }
    
    public static List<WeatherAPI.WeatherObject>? GetText(string targetCity, string targetDate)
    {
        lock (Lock)
        {
            if (!Behaviour.Other.AllowWeatherCaching) {return null;}
            return DateExists(targetCity, targetDate) ? Cache[targetCity][targetDate].Parsed : null;
        }
    }

    // Attaches rich images to an already-cached entry. Does NOT touch entry.Update,
    // so the images expire together with the original text data (same TTL/cleanup cycle).
    // If the underlying entry has already expired/been removed by the time the images
    // are ready, this is a no-op and returns false — there's nothing left to attach to.
    public static bool AddRichImages(string targetCity, string targetDate, List<byte[]> richImages)
    {
        lock (Lock)
        {
            if (!Behaviour.Other.AllowWeatherCaching) {return false;}
            
            if (!DateExists(targetCity, targetDate))
                return false;

            Cache[targetCity][targetDate].RichImages = richImages;
            return true;
        }
    }

    public static List<InputRichMessageMedia>? GetRichImages(string targetCity, string targetDate)
    {
        lock (Lock)
        {
            if (!Behaviour.Other.AllowWeatherCaching) {return null;}
            
            if (!DateExists(targetCity, targetDate))
                return null;

            var data = Cache[targetCity][targetDate].RichImages;

            return data?.Select((bytes, i) => new InputRichMessageMedia
            {
                Id = $"w{i + 1}",
                Media = new InputMediaPhoto(new MemoryStream(bytes))
            }).ToList();
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    // Service Cleanup
    public static void StartCleanupThread(TimeSpan? checkInterval = null) {
        if (!Behaviour.Other.AllowWeatherCaching) return;
        
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
            Name = "WeatherCacheCleanup"
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
            var emptyUids = new List<string>();

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

            foreach (var targetCity in emptyUids)
                Cache.Remove(targetCity);
        }
    }


}