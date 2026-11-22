using System.Net;
using Scheder.Services.JournalAPI.PreFetch;
using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools;
using static Scheder.Tools.Logger;

namespace Scheder.Services.JournalAPI;

public class GetSched
{
    private const MetricType Metric = MetricType.Analyze;

    public static async Task<BestDayOption.BestDayParseResult> GetDay(long uid, (string, string) day, PerformanceMetric? metric, bool fromGroup=false, bool ignoreEarlyDay = false)
    {
        using (metric?.Measure(Metric)) {
            return await BestDayOption.Get(uid, day, fromGroup, ignoreEarlyDay);
        }
    }
    public static async Task<BestDayOption.BestDayParseResult> GetForcedDay(long uid, (string, string) day, PerformanceMetric? metric, bool fromGroup=false)
    {
        
        using (metric?.Measure(Metric)) {
            return await BestDayOption.Get(uid, day, fromGroup, true, true);
        }
    }



    private static async Task<(string? sched, int code)> GetSchedFromApi(
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
            if (newId == null) {return (null, 0);}
            
            id = (long)newId;
        }
        
        var (token, _) = recommendedToken is null ? await TokenService.Get(id, fromGroup, parent: uid) : (recommendedToken, ["?"]);
        if (token == null) return (null, 0);
        var startDate = dayData.StartDate;
        var endDate = dayData.EndDate;

        try
        {
            var response = await API.GetSched(token, startDate, endDate, metric: metric);

            if (response.Code != (int)HttpStatusCode.OK)
            {
                Log.Warning("[GetSched | {uid}] Journal API returned {code}: {body}",
                    uid, response.Code, response.Message);
                return (null, response.Code);
            }

            if (string.IsNullOrEmpty(response.Message))
            {
                Log.Warning("[GetSched | {uid}] Journal API returned an empty schedule body", uid);
                return (null, 0);
            }

            return (response.Message, response.Code);
        }
        catch (Exception e)
        {
            // Timeout or smth
            Log.Error("[GetSched | {uid}] Journal API schedule request failed: {error}", uid, e.Message);
            return (null, 0);
        }
    }


    public static async Task<API.ExamsResponse?> GetExamsFromApi(
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

        var (token, _) = recommendedToken is null ? await TokenService.Get(id, fromGroup, parent: uid) : (recommendedToken, ["?"]);
        if (token == null) return null;

        try
        {
            var response = await API.GetExams(token, metric: metric);

            if (!response.MainSuccess)
            {
                Log.Warning("[GetSched | {uid}] Journal API exams returned {codes}",
                    uid, string.Join(", ", response.Codes));
                return null;
            }

            return response;
        }
        catch (Exception e)
        {
            Log.Error("[GetSched | {uid}] Journal API exams request failed: {error}", uid, e.Message);
            return null;
        }
    }

    
    private static async Task<(string? sched, API.ExamsResponse? exams, int code)> FetchSchedAndExams(
        long uid,
        BestDayOption.BestDayParseResult dayData,
        bool fromGroup,
        string token,
        PerformanceMetric? metric)
    {
        var result = await ParallelTasks.Run(
            GetSchedFromApi(uid, dayData, fromGroup, token, metric: metric),
            GetExamsFromApi(uid, dayData, fromGroup, token, metric: metric)
        );

        return (result.Item1.sched, result.Item2, result.Item1.code);
    }



    public static async Task<(string?, API.ExamsResponse?, string[])> GetSchedAndExams(
        long uid,
        BestDayOption.BestDayParseResult dayData,
        bool fromGroup = false,
        string? recommendedToken = null,
        PerformanceMetric? metric = null
    ) {
        var allowCache =
            await SettingsService.GetBool(uid, SettingsTypeList.AllowDataCaching, fromGroup, CancellationToken.None);

        var cacheDate = $"{dayData.StartDate} — {dayData.EndDate}";
        var (cachedSched, cachedExams) =
            CachedScheduleLibrary.DateExists(uid, cacheDate)
                ? CachedScheduleLibrary.GetText(uid, cacheDate)
                : (null, null);

        if (cachedSched != null && cachedExams != null && allowCache) {
            Log.Debug("[CachedScheduleLibrary | {uid}] Sending cached response!", uid);
            return (cachedSched, cachedExams, ["-"]);
        }

        long? boundToken = fromGroup ? await CodeBunch.GetUidFromGroup(uid) : uid;

        if (boundToken == null) {
            return (null, null, ["No bounded person for group"]);
        }

        (recommendedToken, var jwt) =
            recommendedToken is null
                ? await TokenService.Get((long)boundToken, fromGroup, parent: uid, metric: metric)
                : (recommendedToken, ["?"]);


        // fix for double TokenService.Get call
        if (recommendedToken is null) return (null, null, jwt); // if there's no JWT key - return none;

        var (newSched, newExams, code) = await FetchSchedAndExams(uid, dayData, fromGroup, recommendedToken, metric);
        
        if (code == (int)HttpStatusCode.Unauthorized)
        {
            Log.Warning("[GetSched | {uid}] Journal API rejected the token (401), re-authorizing and retrying once", uid);

            recommendedToken = await TokenService.ReAuthorize((long)boundToken, fromGroup, parent: uid, metric: metric);

            if (recommendedToken is null)
            {
                return (null, null, ["401", "re-auth failed"]);
            }

            (newSched, newExams, code) = await FetchSchedAndExams(uid, dayData, fromGroup, recommendedToken, metric);
            jwt = ["ReAuth", $"HTTP {code}"];
        }
        else if (code != (int)HttpStatusCode.OK)
        {
            jwt = [$"HTTP {code}"];
        }


        CachedScheduleLibrary.Delete(uid, cacheDate);
        if (allowCache && code == (int)HttpStatusCode.OK && newSched is not null) {
            CachedScheduleLibrary.Add(uid, cacheDate, newSched, newExams);
        }


        return (newSched, newExams, jwt);

    }
}
