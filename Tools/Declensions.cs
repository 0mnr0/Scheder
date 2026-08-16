namespace Scheder.Tools;

public class Declensions {
    
    
    private static Dictionary<int, string> _mAndD = new() {
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



    public static string GetDeclensionDayTitle(BestDayOption.BestDayParseResult day) {
        var targetDay = day.DateStart;
        return GetDeclensionDayTitle(targetDay);
    }

    public static string GetDeclensionDayTitle(DateTime day) {
        var monthName = _mAndD[day.Month];
        return $"{day.Day} {monthName}";
    }
}