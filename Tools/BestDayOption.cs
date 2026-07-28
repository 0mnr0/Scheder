using Scheder.Services.ContextDetection;

namespace Scheder.Tools;

public static class BestDayOption
{
    public static async Task<BestDayParseResult> Get(long uid, string day, bool fromGroup = false, bool ignoreEarlyDay = false, bool asForced = false)
    {
        var timeShift = await GmtTool.Get(uid, fromGroup); // (from -24 to +24 hours int)
        var clientHour = DateTime.Now.AddHours(timeShift);
        var response = new BestDayParseResult();
        
        var date = DateTime.Now;
        if (day is DayType.Tomorrow or DayType.ReTomorrow && clientHour.Hour <= 1 && !ignoreEarlyDay && !asForced)
        {
            // Логика в том, что люди, зачастую, при наступлении нового дня
            // не соображают об этом "сразу" и могут спросить расписание на завтра, когда на самом деле, тот хочет получить расписание на сегодня
            // Поэтому фикс прост - если запрос пришёл до 2‑х (01:59 actually) часов ночи - сделать так, чтобы был сдвиг на один день назад
            date = date.AddDays(-1);
            
            response.dayDiff = day switch {
                DayType.Tomorrow => 1,
                DayType.ReTomorrow => 2,
                _ => response.dayDiff
            };
            response.dayDisplay = day switch {
                DayType.Tomorrow => DayType.Today,
                DayType.ReTomorrow => DayType.Tomorrow,
                _ => response.dayDisplay
            };
            
            response.IsEarlyDayMoveFix = true;
        }

        
        switch (day)
        {
            case DayType.Tomorrow:
                date = date.AddDays(1);
                break;
                
            case DayType.ReTomorrow:    
                date = date.AddDays(2);
                break;
                
            case DayType.Yesterday:
                date = date.AddDays(-1);
                break;
                
            case DayType.ReYesterday:
                date = date.AddDays(-2);
                break;
            
            case DayType.Monday:
            case DayType.Tuesday:
            case DayType.Wednesday:
            case DayType.Thursday:
            case DayType.Friday:
            case DayType.Saturday:
            case DayType.Sunday:
                date = GetNearestDay(clientHour, day);
                break;
            
        }
        
        
        
        // another one for setting response types
        switch (day)
        {
            case DayType.Today:
            case DayType.Tomorrow:
            case DayType.ReTomorrow:
            case DayType.Yesterday:
            case DayType.ReYesterday:
            case DayType.Monday:
            case DayType.Tuesday:
            case DayType.Wednesday:
            case DayType.Thursday:
            case DayType.Friday:
            case DayType.Saturday:
            case DayType.Sunday:
                response.DateStart = date;
                response.DateEnd = date;
                break;
            
            case DayType.Week:
                response.DateStart = date;
                response.DateEnd = date.AddDays(6);
                response.IsWeek = true;
                break;
            
            case DayType.NextWeek:
                response.DateStart = date.AddDays(6);
                response.DateEnd = date.AddDays(12);
                response.IsWeek = true;
                break;
            
            case DayType.PrevWeek:
                response.DateStart = date.AddDays(-12);
                response.DateEnd = date.AddDays(-6);
                response.IsWeek = true;
                break;
        }
        
        response.StartDate = response.DateStart.ToString("yyyy-MM-dd");
        response.EndDate = response.DateEnd.ToString("yyyy-MM-dd");
        if (!response.IsEarlyDayMoveFix) response.dayDisplay = day;
        response.dayParsedName = DateExtractor.GetDayName(day);
        response.dayType = day;
        
        return response;
    }

    private static DateTime GetNearestDay(DateTime date, string dayName)
    {
        if (!Enum.TryParse<DayOfWeek>(dayName, out var targetDay))
            throw new ArgumentException($"Unknown day name: {dayName}", nameof(dayName));

        var daysToAdd = ((int)targetDay - (int)date.DayOfWeek + 7) % 7;

        return date.AddDays(daysToAdd);
    }


    public class BestDayParseResult
    {
        public string dayType { get; set; }
        public string dayParsedName { get; set; } = "";
        public string dayDisplay { get; set; } = "";
        public int dayDiff { get; set; } = 0;
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public bool IsWeek { get; set; }
        public bool IsEarlyDayMoveFix { get; set; }
    }
}