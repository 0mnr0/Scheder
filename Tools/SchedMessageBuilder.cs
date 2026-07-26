using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scheder.Config;
using Scheder.ContextDetection;
using Scheder.Services.WebRender;

namespace Scheder.Tools;

public abstract class SchedMessageBuilder
{
    public static string BuildMessage(string? raw, BestDayOption.BestDayParseResult day, string? rawExamList = null)
    {
        
        if (string.IsNullOrEmpty(raw))
        {
            return $"""
                    <b> <i> Токен не получен :/ </i> </b>
                    <details>
                        <summary> Детали: </summary>
                        <i>Tries: 3 </i> <br>
                        <i>Codes: [422, 422, 422] </i><br>
                        <i>Is auth down? </i><br>
                    </details>

                    """;
        }

        var displayWeek = day.IsWeek;
        var dayName = DateExtractor.GetDayName(day.dayType);
        var dateDisplay = (displayWeek ? $"{day.StartDate} — {day.EndDate}" : day.StartDate).Replace("-", ".");

        
        var (lessons, exams) = ParseAndSort(raw, rawExamList, day);
        var unixTime = CodeBunch.GetUnixFromDateTime(day, lessons);

        var blocks = new List<string> {
            $"""
              <h4> <b> Пары на {dayName}: </b> </h4>
              <a> (<tg-time unix="{unixTime}" format="wDT">{dateDisplay}</tg-time>, {lessons.Count * 1.5}ч) </a> 
              
              <br>
              <br> {(displayWeek ? "<br>" : "")}
              <hr/>
              """
        };


        var lastDate = "None";
        for (var i = 0; i < lessons.Count; i++)
        {
            var isLast = i == lessons.Count - 1;
            var lesson = lessons[i];
            var lessonDate = lesson.Date;
            if (!string.Equals(lastDate, lessonDate, StringComparison.Ordinal))
            {
                lastDate = lessonDate;
                if (displayWeek)
                {
                    blocks.Add($"<br> <h3> Пары на {lastDate}: </h3>");
                }
            }



            if (!displayWeek)
            {
                blocks.Add($"""
                            <mark> {lesson.LessonIndex} </mark> <b> {lesson.TeacherName} </b> 
                            <blockquote> 
                                <b>{lesson.SubjectName}</b>
                                <br>
                                {lesson.StartedAt} — {lesson.FinishedAt} | {lesson.RoomName}
                            </blockquote>
                            {(isLast ? "" : "<br>")}

                            """);
            }
            else
            {
                blocks.Add($"""
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

        if (lessons.Count == 0)
        {
            blocks.Add($"""
                        <br>
                        
                        <table bordered striped>
                            <thead>
                                <tr><th colspan="2" align="center">Пар на {day.StartDate} не найдено! </th></tr>
                            </thead>
                        </table>
                        
                        <br>
                        """);
        }

        if (day.IsEarlyDayMoveFix)
        {
            blocks.Add($"""
                        <details>
                            <summary> ⚠️ Внимание! </summary>
                            
                            <blockquote> <b> ⚠️ Вы указали параметр "{DateExtractor.GetDayName(day.dayType)}". </b> <br>
                            В вашем регионе менее двух часов назад сменился календарный день, иногда люди не сразу это осознают поэтому существует этот механизм.<br>
                            До двух часов ночи "завтра" засчитывается как "сегодня", а "послезавтра" - как "завтра".<br>
                            Чтобы избежать эту механику добавьте "!" в конец сообщения. Например: "пары завтра!". Тогда механизм не сработает и вы, в час ночи, получите расписание на завтра.
                            
                            </blockquote>
                        </details>

                        """);
        }
        
        if (exams.Count > 0)
        {
            blocks.Add($"""
                        <br><hr/>
                        <details>
                        <summary> <b> ⚠️ Экзамены ({exams.Count}) </b> </summary>
                        
                        """);
            for (var i = 0; i < exams.Count; i++)
            {
                var isLast = i == exams.Count - 1;
                var exam = exams[i];
                
                blocks.Add($"""
                            <table bordered>
                                <thead>
                                    <tr><th align="center"> <b> {i+1}) {exam.TeacherName}</b>  </th></tr>
                                </thead>
                                <tbody>
                                    <tr><th align="center"> {exam.SpecName} </th></tr>
                                    {(displayWeek ? $"""<tr><th align="center">Дата: {exam.Date}</th></tr>""" : "")}
                                </tbody>
                            </table>
                            """);

            }
            
            blocks.Add("</details>");
        }

        return string.Join("", blocks);

    }


    private static (List<Lesson>, List<ExamObject>) ParseAndSort(string json, string? jsonExams, BestDayOption.BestDayParseResult day)
    {
        var lessons = JsonSerializer.Deserialize<List<Lesson>>(json)
                      ?? [];

        var sched = lessons
            .OrderBy(l => ParseDate(l.Date))
            .ThenBy(l => l.LessonIndex)
            .ToList();
        
        
        var exams = jsonExams != null ? (JsonSerializer.Deserialize<List<ExamObject>>(jsonExams) ?? []) : [];

        var examsList = exams
            .OrderBy(l => ParseDate(l.Date))
            .ToList();
        
        var dayStart = DateOnly.Parse(day.StartDate);
        var dayEnd = DateOnly.Parse(day.EndDate);

        examsList.RemoveAll(e =>
        {
            var d = DateOnly.Parse(e.Date);
            return d < dayStart || d > dayEnd;
        });

        return (sched, examsList);
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
            result += $"{w.WeatherTextIcon} <b> {w.Time} — </b> {Convert.ToInt32(w.Temp)}° <br>\n";
        }

        result += "\n";
        return result;
    }
    

    public static async Task<List<byte[]>> BuildWeather(long chatId, BestDayOption.BestDayParseResult dayParseResult, bool isGroup) {
        if (isGroup && !Behaviour.Groups.AllowWeatherImageOutput) return [];
        
        var weatherData = await Weather.GetWeather(chatId, dayParseResult, isGroup);
        if (weatherData == null) {
            return [];
        }
        
        return await WebRender.RenderWeather(weatherData);
    }
}