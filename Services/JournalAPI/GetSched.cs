using Scheder.Services.JournalAPI.PreFetch;
using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools;
using static Scheder.Tools.Logger;

namespace Scheder.Services.JournalAPI;

public class GetSched
{
    private const MetricType Metric = MetricType.Analyze;
    private const MetricType MetricParse = MetricType.DataParse;
    public static async Task<BestDayOption.BestDayParseResult> GetDay(long uid, string day, PerformanceMetric? metric, bool fromGroup=false, bool ignoreEarlyDay = false)
    {
        using (metric?.Measure(Metric)) {
            return await BestDayOption.Get(uid, day, fromGroup, ignoreEarlyDay);
        }
    }
    public static async Task<BestDayOption.BestDayParseResult> GetForcedDay(long uid, string day, PerformanceMetric? metric, bool fromGroup=false)
    {
        
        using (metric?.Measure(Metric)) {
            return await BestDayOption.Get(uid, day, fromGroup, true, true);
        }
    }


    private static async Task<string?> GetSchedFromApi(
            long uid,
            BestDayOption.BestDayParseResult dayData,
            bool fromGroup = false,
            string? recommendedToken = null,
            PerformanceMetric? metric = null
        )
    {
        var id = uid;
        if (fromGroup)
        {
            var newId = await CodeBunch.GetUidFromGroup(uid);
            if (newId == null) {return null;}
            
            id = (long)newId;
        }
        
        var (token, _) = recommendedToken is null ? await TokenService.Get(id, parent: uid) : (recommendedToken, ["?"]);
        if (token == null) return null;
        var startDate = dayData.StartDate;
        var endDate = dayData.EndDate;

        var response = await API.GetSched(token, startDate, endDate, metric: metric);
        var newSched = response.Message;
        
        
        return newSched;
    }


    public static async Task<string?> GetExamsFromApi(
        long uid,
        BestDayOption.BestDayParseResult dayData,
        bool fromGroup = false,
        string? recommendedToken = null,
        PerformanceMetric? metric = null
    ) {
        var id = uid;
        if (fromGroup) {
            var newId = await CodeBunch.GetUidFromGroup(uid);
            if (newId == null) {
                return null;
            }

            id = (long)newId;
        }

        var (token, _) = recommendedToken is null ? await TokenService.Get(id, parent: uid) : (recommendedToken, ["?"]);
        if (token == null) return null;
        
        var response = await API.GetExams(token, metric: metric);
        var examsList = response.Message;

        return examsList;
    }



    public static async Task<(string?, string?, string[])> GetSchedAndExams(
        long uid,
        BestDayOption.BestDayParseResult dayData,
        bool fromGroup = false,
        string? recommendedToken = null,
        PerformanceMetric? metric = null
    )
    {
        var allowCache = await SettingsService.GetBool(uid, SettingsList.AllowDataCaching, CancellationToken.None);
        
        var cacheDate = $"{dayData.StartDate} — {dayData.EndDate}";
        var (cachedSched, cachedExams) =
            CachedScheduleLibrary.DateExists(uid, cacheDate)
                ? CachedScheduleLibrary.GetText(uid, cacheDate)
                : (null, null);

        if (cachedSched != null && cachedExams != null && allowCache)
        {
            Log.Information("[CachedScheduleLibrary | {uid}] Sending cached response!", uid);
            return (cachedSched, cachedExams, ["-"]);
        }

        var token4 = (long)(fromGroup ? await CodeBunch.GetUidFromGroup(uid) : uid)!;
        (recommendedToken, var jwt) =
            recommendedToken is null ? await TokenService.Get(token4, parent: uid) : (recommendedToken, ["?"]);


        // fix for double TokenService.Get call
        if (recommendedToken is null) return (null, null, jwt); // if there's no JWT key - return none;

        var (newSched, newExams) = await ParallelTasks.Run(
            GetSchedFromApi(uid, dayData, fromGroup, recommendedToken, metric: metric),
            GetExamsFromApi(uid, dayData, fromGroup, recommendedToken, metric: metric)
        );

        
        CachedScheduleLibrary.Delete(uid, cacheDate);
        if (allowCache)
        {
            CachedScheduleLibrary.Add(uid, cacheDate, newSched, newExams);
        }


        return (newSched, newExams, jwt);
    
}






}