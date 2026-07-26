using Scheder.Tools;

namespace Scheder.JournalAPI;

public class GetSched
{
    public static async Task<BestDayOption.BestDayParseResult> GetDay(long uid, string day, bool fromGroup=false, bool ignoreEarlyDay = false)
    {
        return await BestDayOption.Get(uid, day, fromGroup, ignoreEarlyDay=ignoreEarlyDay);
    }


    private static async Task<string?> GetSchedFromApi(long uid, BestDayOption.BestDayParseResult dayData, bool fromGroup = false, string? recommendedToken = null)
    {
        var id = uid;
        if (fromGroup)
        {
            var newId = await CodeBunch.GetUidFromGroup(uid);
            if (newId == null) {return null;}
            
            id = (long)newId;
        }
        
        var token = recommendedToken ?? await TokenService.Get(id);
        if (token == null) return null;
        var startDate = dayData.StartDate;
        var endDate = dayData.EndDate;

        var response = await API.GetSched(token, startDate, endDate);
        var newSched = response.Message;
        
        
        return newSched;
    }


    private static async Task<string?> GetExamsFromApi(long uid, BestDayOption.BestDayParseResult dayData, bool fromGroup = false, string? recommendedToken = null)
    {
        var id = uid;
        if (fromGroup)
        {
            var newId = await CodeBunch.GetUidFromGroup(uid);
            if (newId == null) {return null;}
            
            id = (long)newId;
        }
        
        var token = recommendedToken ?? await TokenService.Get(id);
        if (token == null) return null;
       
        
        var response = await API.GetExams(token);
        var examsList = response.Message;
        
        return examsList;
    }
    
    
    
    public static async Task<(string?, string?)> GetSchedAndExams(
        long uid,
        BestDayOption.BestDayParseResult dayData,
        bool fromGroup = false,
        string? recommendedToken = null
        )
    {
        
        var cacheDate = $"{dayData.StartDate} — {dayData.EndDate}";
        var (cachedSched, cachedExams) =
            CachedScheduleLibrary.DateExists(uid, cacheDate)
                ? CachedScheduleLibrary.GetText(uid, cacheDate)
                : (null, null);
        
        if (cachedSched != null && cachedExams !=null)
        {
            Console.WriteLine("[CachedScheduleLibrary] Sending cached response!");
            return (cachedSched, cachedExams);
        }
        
        
        var (newSched, newExams) = await ParallelTasks.Run(
            GetSchedFromApi(uid, dayData, fromGroup, recommendedToken),
            GetExamsFromApi(uid, dayData, fromGroup, recommendedToken)
        );
        CachedScheduleLibrary.Delete(uid, cacheDate);
        CachedScheduleLibrary.Add(uid, cacheDate, newSched, newExams);
        
        return (newSched, newExams);

    }






}