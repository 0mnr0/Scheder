using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scheder.Services.ContextDetection;
using Scheder.Services.Weather;
using Scheder.Services.WebRender;
using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools.Config;
using Telegram.Bot.Types;

namespace Scheder.Tools;

public abstract class SchedMessageBuilder
{
    public static string BuildMessage(
        string? raw,
        BestDayOption.BestDayParseResult day,
        string? rawExamList = null,
        string[]? jwtData = null,
        PerformanceMetric? metric = null,
        bool asChange = false,
        bool showDateInTitle = false
    )
    {
        using (metric?.Measure(MetricType.Build)) {
            if (string.IsNullOrEmpty(raw))
            {
                return BuildJwtFail(jwtData);
            }

            if (asChange) { showDateInTitle = true;}
            if (day.ExactDate) { showDateInTitle = true;}
            var displayWeek = day.IsWeek;
            var dayName = showDateInTitle ? day.dayDisplay : DateExtractor.GetDayName(day.dayDisplay);
            var dateDisplay = (displayWeek ? $"{day.StartDate} — {day.EndDate}" : day.StartDate).Replace("-", ".");

            var (lessons, exams) = ParseAndSort(raw, rawExamList, day);
            var messageSize = 512
                              + lessons.Count * (displayWeek ? 420 : 250)
                              + exams.Count * 200;

            var messageText = new StringBuilder(capacity: messageSize);

            var unixTime = CodeBunch.GetUnixFromDateTime(day, lessons);

            if (asChange) {
                messageText.Append($"<h4> Изменились пары на {dayName}: </h4>");
            }
            else {
                messageText.Append($"<h4> <b> Пары на {dayName}: </b> </h4>");
            }
            
            messageText.Append(
                $"""
                 <a> (<tg-time unix="{unixTime}" format="wDT">{dateDisplay}</tg-time>, {lessons.Count * 1.5}ч) </a> 

                 <br>
                 <br> {(displayWeek ? "<br>" : "")}
                 <hr/>
                 """
            );


            var lastDate = "None";
            for (var i = 0; i < lessons.Count; i++) {
                var isLast = i == lessons.Count - 1;
                var lesson = lessons[i];
                var lessonDate = lesson.Date;
                if (!string.Equals(lastDate, lessonDate, StringComparison.Ordinal)) {
                    lastDate = lessonDate;
                    if (displayWeek) {
                        messageText.Append($"<br> <h3> Пары на {lastDate}: </h3>");
                    }
                }



                if (!displayWeek) {
                    messageText.Append($"""
                                        <mark> {lesson.LessonIndex} </mark> <b> {lesson.TeacherName} </b> 
                                        <blockquote> 
                                            <b>{lesson.SubjectName}</b>
                                            <br>
                                            {lesson.StartedAt} — {lesson.FinishedAt} | {lesson.RoomName}
                                        </blockquote>
                                        {(isLast ? "" : "<br>")}

                                        """);
                }
                else {
                    messageText.Append($"""
                                        <details>
                                        <summary>{lesson.StartedAt} - {lesson.SubjectName}</summary>
                                            <table bordered striped>
                                                <thead>
                                                    <tr><th colspan="2" align="center"><h2><b>{lesson.SubjectName}</b></h2></th></tr>
                                                    <tr><th colspan="2" align="center">{lesson.TeacherName}</th></tr>
                                                </thead>
                                            
                                                <tbody>
                                                    <tr>
                                                        <td align="center">С {lesson.StartedAt} по {lesson.FinishedAt}</td>
                                                        <td align="center">В {lesson.RoomName}</td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                            {(isLast ? "" : "<br>")}
                                        </details>
                                        """);

                    /*<b> Пара №{lesson.lesson} </b> <br>
                    <b> Учитель: {lesson.teacher_name} </b> <br>
                    <br>
                    <a> Пара: {lesson.subject_name} </a> <br>
                    <a> Время:{lesson.started_at} - {lesson.finished_at}</a> <br>
                    <a> Аудитория: {lesson.room_name} </a>*/
                }
            }

            if (lessons.Count == 0) {
                messageText.Append($"""
                                    <br>

                                    <table bordered striped>
                                        <thead>
                                            <tr><th colspan="2" align="center">Пар на {day.StartDate} не найдено! </th></tr>
                                        </thead>
                                    </table>

                                    <br>
                                    """);
            }

            if (day.IsEarlyDayMoveFix) {
                var dayTarget = DateExtractor.GetDayName(day.dayType);
                messageText.Append($"""
                                    <details>
                                        <summary> ⚠️ Внимание! </summary>
                                        
                                        <blockquote> <b> ⚠️ Вы указали параметр "{dayTarget}". </b> <br>
                                        В вашем регионе менее двух часов назад сменился календарный день, иногда люди не сразу это осознают поэтому существует этот механизм.<br>
                                        До двух часов ночи "завтра" засчитывается как "сегодня", а "послезавтра" - как "завтра".<br>
                                        Чтобы избежать эту механику добавьте "!" в конец сообщения (Например: "пары завтра!") или используйте кнопку ниже для принудительного показа расписания "на {dayTarget}". Тогда механизм не сработает и вы, в час ночи, получите расписание на завтра.
                                        
                                        </blockquote>
                                    </details>

                                    """);
            }

            if (exams.Count > 0) {
                messageText.Append(
                    BuildExams(rawExamList, day, showDates: displayWeek)
                );
            }

            return messageText.ToString();
        }

    }









    public static string BuildExams(
            string? raw,
            BestDayOption.BestDayParseResult day,
            bool showDates = false,
            bool isStandalone = false,
            PerformanceMetric? metric = null
        ) {
        using (metric?.Measure(MetricType.Build)) {
            var examList = ParseAndSortExams(raw, day, skipDateEnd: isStandalone);
            var messageText = new StringBuilder(220 * examList.Count + 200);

            switch (isStandalone) {
                case true:
                    messageText.Append("<h4> Предстоящие экзамены: </h4>\n\n");
                    break;
                case false:
                    messageText.Append($"""
                                        <br><hr/>
                                        <details>
                                        <summary> <b> ⚠️ Экзамены ({examList.Count}) </b> </summary>

                                        """);
                    break;
            }


            for (var i = 0; i < examList.Count; i++) {
                var exam = examList[i];

                messageText.Append($"""
                                    <table bordered>
                                        <thead>
                                            <tr><th align="center"> <b> {i + 1}) {exam.TeacherName}</b>  </th></tr>
                                        </thead>
                                        <tbody>
                                            <tr><th align="center"> {exam.SpecName} </th></tr>
                                            {(showDates ? $"""<tr><th align="center">Дата: {exam.Date}</th></tr>""" : "")}
                                        </tbody>
                                    </table>
                                    """);

            }

            switch (isStandalone) {
                case false:
                    messageText.Append("</details>");
                    break;
                case true when examList.Count == 0:
                    messageText.Append($"""
                                        <br>

                                        <table bordered striped>
                                            <thead>
                                                <tr><th align="center"> Пусто 🤷 </th></tr>
                                            </thead>
                                        </table>
                                        """);
                    break;
            }

            return messageText.ToString();
        }
    }



    public static InputRichMessage AddWeather(string input, (List<byte[]>, List<InputRichMessageMedia>?) weatherImages, bool isExams = false) {
        input += """
                    <h5> Погода: </h5> 
                    <tg-slideshow>
                        <img src="tg://photo?id=w1">
                        <img src="tg://photo?id=w2">
                    </tg-slideshow>
                 """;

        var useCache = weatherImages.Item2 != null;
        
        List<InputRichMessageMedia> mediaList = [];
        if (!useCache) {
            var finalWeather = weatherImages.Item1;
            var stream1 = new MemoryStream(finalWeather[0]);
            var stream2 = new MemoryStream(finalWeather[1]);
            mediaList.AddRange(
            [
                new InputRichMessageMedia
                    { Id = "w1", Media = new InputMediaPhoto(stream1 )},
                new InputRichMessageMedia
                    { Id = "w2", Media = new InputMediaPhoto(stream2 )},
            ]);
        }
        else {
            mediaList.AddRange(weatherImages.Item2!);
        }
        
        return new InputRichMessage {
            Html = input,
            Media = mediaList
        };

    }





    private static (List<Lesson>, List<ExamObject>) ParseAndSort(string json, string? jsonExams, BestDayOption.BestDayParseResult day)
    {
        var lessons = JsonSerializer.Deserialize<List<Lesson>>(json)
                      ?? [];

        var sched = lessons
            .OrderBy(l => ParseDate(l.Date))
            .ThenBy(l => l.LessonIndex)
            .ToList();
        
        return (sched, ParseAndSortExams(jsonExams, day));
    }

    private static List<ExamObject> ParseAndSortExams(string? jsonExams, BestDayOption.BestDayParseResult day, bool skipDateEnd = false)
    {
        
        var exams = jsonExams != null ? (JsonSerializer.Deserialize<List<ExamObject>>(jsonExams) ?? []) : [];

        var examsList = exams
            .OrderBy(l => ParseDate(l.Date))
            .ToList();
        
        var dayStart = DateOnly.Parse(day.StartDate);
        var dayEnd = DateOnly.Parse(day.EndDate);

        examsList.RemoveAll(e =>
        {
            var d = DateOnly.Parse(e.Date);
            return (skipDateEnd ? d < dayStart : d < dayStart || d > dayEnd);
        });

        return examsList;
    }
    
    private static DateTime ParseDate(string date)
    {
        if (DateTime.TryParse(date, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
        {
            return result;
        }
        
        return DateTime.TryParseExact(date, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result) ? result : DateTime.MinValue;
    }











    public class Lesson // DO NOT MAKE IT ABSTRACT!
    {

        [JsonPropertyName("lesson")]
        public int LessonIndex { get; set; }
        
        [JsonPropertyName("date")]
        public required string Date { get; set; }
        
        [JsonPropertyName("room_name")]
        public string? RoomName { get; set; }
        
        [JsonPropertyName("started_at")]
        public string? StartedAt { get; set; }
        
        [JsonPropertyName("finished_at")]
        public string? FinishedAt { get; set; }
        
        [JsonPropertyName("subject_name")]
        public string? SubjectName { get; set; }
        
        [JsonPropertyName("teacher_name")]
        public string? TeacherName { get; set; }
    }

    public class ExamObject // DO NOT MAKE IT ABSTRACT!
    {

        [JsonPropertyName("date")]
        public required string Date { get; set; }
        
        [JsonPropertyName("spec")]
        public required string SpecName { get; set; }
        
        [JsonPropertyName("teacher")]
        public required string TeacherName { get; set; }
    }




    

    public static async Task<string?> BuildWeatherText(long chatId, BestDayOption.BestDayParseResult dayParseResult, bool isGroup) {
        var weatherData = await Weather.GetWeather(chatId, dayParseResult, isGroup);
        if (weatherData == null) {
            return null;
        }

        var result = "\n<h5> Погода: </h5>";
        foreach (var w in weatherData) {
            result += $"{w.WeatherTextIcon} <b> {w.Time} — </b>{Convert.ToInt32(w.Temp)}° <br>\n";
        }

        result += "\n";
        return result;
    }
    

    public static async Task<(List<byte[]>, List<InputRichMessageMedia>?)> BuildWeather(
            long chatId,
            BestDayOption.BestDayParseResult dayParseResult,
            bool isGroup,
            CancellationToken cancellationToken,
            PerformanceMetric? metric = null
        ) {
            var weatherSettings = await SettingsService.GetValue(chatId, SettingsTypeList.AllowWeather, isGroup, cancellationToken);
            var isWeatherAllowed = weatherSettings is not 0;

            if (isGroup && !Behaviour.Groups.AllowWeatherImageOutput || !isWeatherAllowed) return ([], null);

            var cachedWeatherUrlIds = await Weather.GetRichImageUrls(chatId, dayParseResult, isGroup);
            if (cachedWeatherUrlIds is not null && cachedWeatherUrlIds.Count > 0) {
                return ([], cachedWeatherUrlIds);
            }

            var weatherData = await Weather.GetWeather(chatId, dayParseResult, isGroup, metric: metric);
            if (weatherData == null) {
                return ([], null);
            }

            return (await WebRender.RenderWeather(weatherData, metric), null);
    }


    private static string BuildJwtFail(string[]? data)
    {

        var text = new StringBuilder(capacity: 1000);
        text.Append("""
                        <b> <i> Токен не получен :/ </i> </b>
                        <details>
                            <summary> Детали: </summary>
                    """);
        
        if (data is null)
        {
            text.Append("<i> jwtData is null </i>");
        } else
        {
            text.Append($"<p> Tries: {data.Length} </p>");
            text.Append($"<p> Codes: [{string.Join(", ", data)}] </p>");
        }
        
        text.Append("</details>");
        return text.ToString();
    }
}