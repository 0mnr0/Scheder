namespace Scheder.Tools;

public class Declensions {
    
    
    private static readonly Dictionary<int, string> MAndD = new() {
        [1] = "января",
        [2] = "ферваля",
        [3] = "марта",
        [4] = "апреля",
        [5] = "мая",
        [6] = "июня",
        [7] = "июля",
        [8] = "августа",
        [9] = "сентября",
        [10] = "октября",
        [11] = "ноября",
        [12] = "декабря"
    };
    
    
    private static readonly Dictionary<int, string> HoursGmt = new() {
        [0] = "часов",
        [1] = "час",
        [2] = "часа",
        [3] = "часа",
        [4] = "часа",
        [5] = "часов",
        [6] = "часов",
        [7] = "часов",
        [8] = "часов",
        [9] = "часов",
        [10] = "часов",
        [11] = "часов",
        [12] = "часов",
        [13] = "часов",
        [14] = "часов",
        [15] = "часов",
        [16] = "часов",
        [17] = "часов",
        [18] = "часов",
        [19] = "часов",
        [20] = "часов",
        [21] = "час",
        [22] = "часа",
        [23] = "часа",
        [24] = "часа"
    };



    public static string GetDeclensionDayTitle(BestDayOption.BestDayParseResult day) {
        var targetDay = day.DateStart;
        return GetDeclensionDayTitle(targetDay);
    }

    public static string GetDeclensionDayTitle(DateTime day) {
        var monthName = MAndD[day.Month];
        return $"{day.Day} {monthName}";
    }

    public static string GetDeclensionHourGmtTitle(int hour) {
        if (hour < 0) {hour = -hour;}
        return HoursGmt[hour];
    }
}