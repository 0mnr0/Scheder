using System.Globalization;
using System.Text.RegularExpressions;
using FuzzySharp;
using Scheder.Tools;

namespace Scheder.Services.ContextDetection;
public static class DayType
{
    public const string Monday = "Monday";
    public const string Tuesday = "Tuesday";
    public const string Wednesday = "Wednesday";
    public const string Thursday = "Thursday";
    public const string Friday = "Friday";
    public const string Saturday = "Saturday";
    public const string Sunday = "Sunday";
    
    /* technical dates are below: */
    public const string Tomorrow = "TOMORROW";
    public const string ReTomorrow = "ReTOMORROW";
    public const string Today = "TODAY";
    public const string Yesterday = "YESTERDAY";
    public const string ReYesterday = "ReYESTERDAY";
    public const string Week = "WEEK";
    public const string NextWeek = "NextWEEK";
    public const string PrevWeek = "PrevWEEK";
}

public static class DayDefinition {
    public const string Day = "Day";
    public const string Date = "Date";
    public const string Unknown = "Unknown";
}

public static class DateExtractor
{
    private static MetricType _metric = MetricType.Analyze;
    private static readonly Dictionary<string, string> Dataset = new()
    {
        ["понедельник"] = DayType.Monday,
        ["понед"] = DayType.Monday,
        ["пн"] = DayType.Monday,
        ["вторник"] = DayType.Tuesday,
        ["вт"] = DayType.Tuesday,
        ["среда"] = DayType.Wednesday,
        ["ср"] = DayType.Wednesday,
        ["четверг"] = DayType.Thursday,
        ["чт"] = DayType.Thursday,
        ["пятница"] = DayType.Friday,
        ["пт"] = DayType.Friday,
        ["суббота"] = DayType.Saturday,
        ["сб"] = DayType.Saturday,
        ["воскресенье"] = DayType.Sunday,
        ["вс"] = DayType.Sunday,
        ["завтра"] = DayType.Tomorrow,
        ["зв"] = DayType.Tomorrow,
        ["послезавтра"] = DayType.ReTomorrow,
        ["позавчера"] = DayType.ReYesterday,
        ["поза вчера"] = DayType.ReYesterday,
        ["неделю"] = DayType.Week,
        ["неделя"] = DayType.Week,
        
        ["следующей неделе"] = DayType.NextWeek,
        ["следующая неделя"] = DayType.NextWeek,
        ["следущая неделя"] = DayType.NextWeek,
        ["след неделя"] = DayType.NextWeek,
        ["некст неделе"] = DayType.NextWeek,
        
        ["той неделе"] = DayType.PrevWeek,
        ["та неделя"] = DayType.PrevWeek,
        ["предыдущей неделе"] = DayType.PrevWeek,
        ["пред неделе"] = DayType.PrevWeek,
        
        ["сегодня"] = DayType.Today,
        ["вчера"] = DayType.Yesterday
    };

    private static readonly HashSet<string> TriggerSet = new(StringComparer.OrdinalIgnoreCase)
    {
        "пары", "gfhs", "shed", "sched"
    };

    private static readonly List<string> AllWords = Dataset.Keys.ToList();

    private static readonly Regex IsoDateRegex = new(@"^(\d{4})-(\d{1,2})-(\d{1,2})$", RegexOptions.Compiled);
    private static readonly Regex DayMonthYearRegex = new(@"^(\d{1,2})\.(\d{1,2})\.(\d{4})$", RegexOptions.Compiled);
    private static readonly Regex DayMonthRegex = new(@"^(\d{1,2})\.(\d{1,2})$", RegexOptions.Compiled);
    private static readonly Regex DayOnlyRegex = new(@"^(\d{1,2})$", RegexOptions.Compiled);

    private static string? FindClosest(string word)
    {
        var candidates = AllWords.Where(w => Math.Abs(w.Length - word.Length) <= 1).ToList();
        if (candidates.Count == 0)
        {
            candidates = AllWords;
        }
        
        string? best = null;
        var bestScore = 0;
        foreach (var candidate in candidates)
        {
            var score = Fuzz.Ratio(word, candidate);
            if (score <= bestScore) continue;
            
            bestScore = score;
            best = candidate;
        }

        return bestScore > 75 ? best : null;
    }

    /// <summary>
    /// Пытается распознать явную дату в отдельном слове/токене.
    /// Поддерживает форматы: yyyy-MM-dd, dd.MM.yyyy, dd.MM (текущий год), dd (текущие месяц и год).
    /// </summary>
    private static bool TryParseDateWord(string word, out string isoDate)
    {
        isoDate = string.Empty;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var isoMatch = IsoDateRegex.Match(word);
        if (isoMatch.Success)
        {
            var year = int.Parse(isoMatch.Groups[1].Value);
            var month = int.Parse(isoMatch.Groups[2].Value);
            var day = int.Parse(isoMatch.Groups[3].Value);
            return TryBuildDate(year, month, day, out isoDate);
        }

        var dmyMatch = DayMonthYearRegex.Match(word);
        if (dmyMatch.Success)
        {
            var day = int.Parse(dmyMatch.Groups[1].Value);
            var month = int.Parse(dmyMatch.Groups[2].Value);
            var year = int.Parse(dmyMatch.Groups[3].Value);
            return TryBuildDate(year, month, day, out isoDate);
        }

        var dmMatch = DayMonthRegex.Match(word);
        if (dmMatch.Success)
        {
            var day = int.Parse(dmMatch.Groups[1].Value);
            var month = int.Parse(dmMatch.Groups[2].Value);
            return TryBuildDate(today.Year, month, day, out isoDate);
        }

        var dayOnlyMatch = DayOnlyRegex.Match(word);
        if (dayOnlyMatch.Success)
        {
            var day = int.Parse(dayOnlyMatch.Groups[1].Value);
            return TryBuildDate(today.Year, today.Month, day, out isoDate);
        }

        return false;
    }

    private static bool TryBuildDate(int year, int month, int day, out string isoDate)
    {
        isoDate = string.Empty;

        if (month is < 1 or > 12) return false;
        if (year is < 1 or > 9999) return false;
        if (day < 1 || day > DateTime.DaysInMonth(year, month)) return false;

        isoDate = new DateOnly(year, month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    public static (string, string) GetDay(string text, PerformanceMetric? metric)
    {
        using (metric?.Measure(_metric)) {
            var normalized = text.ToLowerInvariant().Replace("/", " ");

            // 1. Точное совпадение по подстроке (быстрый путь)
            var multiWordKeys = Dataset.Keys.Where(k => k.Contains(' ')).OrderByDescending(k => k.Length).ToList();
            foreach (var key in multiWordKeys) {
                if (normalized.Contains(key)) {
                    return (Dataset[key], DayDefinition.Day);
                }
            }

            var words = normalized.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (words.Count > 0 && TriggerSet.Contains(words[0])) {
                words.RemoveAt(0);
            }

            // 2. Явные даты (yyyy-MM-dd, dd.MM.yyyy, dd.MM, dd)
            foreach (var word in words) {
                if (TryParseDateWord(word, out var isoDate)) {
                    return (isoDate, DayDefinition.Date);
                }
            }

            // 3. Fuzzy-проверка биграмм — сравниваем слово к слову
            for (var i = 0; i < words.Count - 1; i++) {
                string? bestKey = null;
                var bestScore = 0;

                foreach (var key in multiWordKeys) {
                    var keyWords = key.Split(' ');
                    if (keyWords.Length != 2) continue;

                    var score1 = Fuzz.Ratio(words[i], keyWords[0]);
                    var score2 = Fuzz.Ratio(words[i + 1], keyWords[1]);
                    var score = Math.Min(score1, score2); // оба слова должны совпадать

                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestKey = key;
                }

                if (bestKey is not null && bestScore > 60) // порог можно подобрать отдельно
                {
                    return (Dataset[bestKey], DayDefinition.Day);
                }
            }

            // 4. Прежняя логика по отдельным словам
            foreach (var word in words) {
                if (Dataset.TryGetValue(word, out var exact)) {
                    return (exact, DayDefinition.Day);
                }

                var match = FindClosest(word);
                if (match is not null && Dataset.TryGetValue(match, out var fuzzy)) {
                    return (fuzzy, DayDefinition.Day);
                }
            }

            return (DayType.Today, DayDefinition.Unknown);
        }
    }

    public static string GetDayName(string dayName)
    {
        return dayName switch
        {
            DayType.Monday => "понедельник",
            DayType.Tuesday => "вторник",
            DayType.Wednesday => "среду",
            DayType.Thursday => "четверг",
            DayType.Friday => "пятницу",
            DayType.Saturday => "субботу",
            DayType.Sunday => "воскресенье",
            DayType.Tomorrow => "завтра",
            DayType.ReTomorrow => "послезавтра",
            DayType.Week => "неделю",
            DayType.NextWeek => "следующую неделю",
            DayType.PrevWeek => "прошедшую неделю",
            DayType.Yesterday => "вчера",
            DayType.ReYesterday => "позавчера",
            _ => "сегодня"
        };
    }

    public static string GetDayNameByDate(string date) {
        var dateTime = DateTime.ParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture
        );

        string[] days = [
            DayType.Monday,
            DayType.Tuesday,
            DayType.Wednesday,
            DayType.Thursday,
            DayType.Friday,
            DayType.Saturday,
            DayType.Sunday
        ];

        return days[(int)dateTime.DayOfWeek];
    }

    public static string GetMonthName(DateTime date)
    {
        var currentMonth = date.Month;
        return currentMonth switch
        {
            1 => "января",
            2 => "февраля",
            3 => "марта",
            4 => "апреля",
            5 => "мая",
            6 => "июля",
            7 => "июня",
            8 => "августа",
            9 => "сентябля",
            10 => "октября",
            11 => "ноября",
            12 => "декабря",
            _ => ""
        };
    }


    public static (string, string) GetForcedDay(string dateValue) {
        
        var date = DateOnly.ParseExact(dateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (date == today)
            return (DayType.Today, DayDefinition.Day);

        if (date == today.AddDays(-1))
            return (DayType.Yesterday, DayDefinition.Day);

        if (date == today.AddDays(-2))
            return (DayType.ReYesterday, DayDefinition.Day);

        if (date == today.AddDays(1))
            return (DayType.Tomorrow, DayDefinition.Day);

        if (date == today.AddDays(2))
            return (DayType.ReTomorrow, DayDefinition.Day);

        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => (DayType.Monday, DayDefinition.Day),
            DayOfWeek.Tuesday => (DayType.Tuesday, DayDefinition.Day),
            DayOfWeek.Wednesday => (DayType.Wednesday, DayDefinition.Day),
            DayOfWeek.Thursday => (DayType.Thursday, DayDefinition.Day),
            DayOfWeek.Friday => (DayType.Friday, DayDefinition.Day),
            DayOfWeek.Saturday =>  (DayType.Saturday, DayDefinition.Day),
            DayOfWeek.Sunday => (DayType.Sunday, DayDefinition.Day),
            _ => (dateValue, DayDefinition.Unknown)
        };
        
    }
}