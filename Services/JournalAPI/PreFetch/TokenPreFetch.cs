using DotNetEnv;
using Scheder.Config;
using Scheder.ContextDetection;
using Scheder.Tools;

namespace Scheder.JournalAPI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

public class TokenPreFetch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static List<CacheIdAllowed> PrefetchData = [];

    /// <summary>
    /// Парсит JSON (либо один объект, либо массив объектов) в список CacheIdAllowed.
    /// </summary>
    public static List<CacheIdAllowed> ParsePrefetchConfig(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var trimmed = json.TrimStart();

        if (trimmed.StartsWith('['))
        {
            var list = JsonSerializer.Deserialize<List<CacheIdAllowed>>(json, JsonOptions);
            return list ?? [];
        }

        var single = JsonSerializer.Deserialize<CacheIdAllowed>(json, JsonOptions);
        return single != null ? [single] : [];
    }

    public static void RunService()
    {

        var thread = new Thread(async void () =>
        {
            var lastMinute = -1;
            while (true)
            {
                var currentMinute = DateTime.Now.Minute;
                if (currentMinute != lastMinute)
                {
                    lastMinute = currentMinute;
                    await OnEveryMinuteCalculations();
                }
                Thread.Sleep(1000);
            }
        });

        thread.IsBackground = true;
        thread.Start();
    }


    public static void RunServiceFromJson(string json)
    {
        var parsed = ParsePrefetchConfig(json);
        PrefetchData = parsed;
        RunService();
    }   

    public static void SetFetchList(List<CacheIdAllowed> list) {
        PrefetchData = list;
        Console.WriteLine("New Prefetch List:"+list.Count);
    }   

    public static async Task ForceUpdateAll()
    {
        Console.WriteLine("[TokenPreFetch] Called force token update. This can take a while...");
        await OnEveryMinuteCalculations(true);
    }

    private static async Task OnEveryMinuteCalculations(bool forceRun = false)
    {
        if (!Behaviour.Other.AllowPreFetch) return;
        
        var currentHourStr = DateTime.Now.ToString("HH");
        var currentMinute = DateTime.Now.Minute;
        foreach (var config in PrefetchData)
        {
            if (forceRun)
            {
                await MeasureWork(config);
                continue;
            }

            if (!config.ParseHourList.TryGetValue(currentHourStr, out var runsPerHour)) continue;
            if (runsPerHour <= 0) continue;


            var interval = 60 / runsPerHour;
            if (currentMinute % interval == 0)
            {
                await MeasureWork(config);
            }
        }
    }

    private static async Task MeasureWork(CacheIdAllowed config)
    {
        var isGroup = config.IsGroup;
        var uid = config.Id;
        if (isGroup)
        {
            var newUid = await CodeBunch.GetUidFromGroup(uid);
            Console.WriteLine($"[ {DateTime.Now:HH:mm:ss} ] newUid: "+(newUid==null));  
            if (newUid == null) {return;}
            uid = (long)newUid;
        }

        var token = await TokenService.Get(uid, cacheUpdate: true);
        if (token == null) return;

        // PARSE AND CACHE SCHED (Today And Tomorrow)
        List<CopyObject> parseDatesFor = [new() {Id = uid, IsGroup = isGroup}];
        parseDatesFor.AddRange(config.CopyFor);

        Console.WriteLine("Range: "+parseDatesFor.Count);


        foreach (var copyObject in parseDatesFor)
        {
            List<string> parseDatesList = [DayType.Today, DayType.Tomorrow];
            foreach (var parseDate in parseDatesList)
            {
                var day = await GetSched.GetDay(copyObject.Id, parseDate, copyObject.IsGroup);
                await GetSched.GetSchedAndExams(copyObject.Id, day, fromGroup: copyObject.IsGroup, recommendedToken: token);
            }
        }

        Console.WriteLine(string.Join(", ", CachedScheduleLibrary.Cache.Keys));
        Console.WriteLine($"[ {DateTime.Now:HH:mm:ss} ] Скрипт measureWork получил токен для: [{config.Id}]!");

    }


    public class CacheIdAllowed
    {
        public long Id { get; set; }
        public Dictionary<string, int> ParseHourList { get; set; } = new();
        public bool IsGroup { get; set; }
        public CopyObject[] CopyFor { get; set; } = [];
    }

    public class CopyObject
    {
        public long Id { get; set; }
        public bool IsGroup { get; set; } = false;
    }
}