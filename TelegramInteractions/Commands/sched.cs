using Scheder.Services.ContextDetection;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.Services.Weather;
using Scheder.TelegramInteractions.Attributes;
using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Commands;

[Command("/sched", "/shed", "/пары", "/пары")]
public class Sched : ICommand
{

    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken) 
    {
        if (message.Text == null) return;
        var chatId = message.Chat.Id;
        var msgText = message.Text;
        var fromGroup = !ChatTools.IsPrivateChat(message);
        var isGroup = ChatTools.IsGroup(message);
        var isPrivateChat = !isGroup;
        var uniqueDraft = (int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var threadId = ChatTools.GetForumId(message);
        var noHumanoidFixes = msgText[^1].ToString() == "!";
        var draftToken = new CancellationTokenSource();
        var metric = new PerformanceMetric();
        metric.Start(MetricType.Total);
        
        var calledViaContext = args is ["directMessage", _]; // checks that .length == 2 and first index is "directMessage"

        if (isGroup && !await Memory.Group.IsGroupBind(chatId)) {
            await bot.SendMessage(
                chatId: chatId,
                "Группа не привязана к аутентификационным данным. Используйте\n\n<blockquote> /bindgroup </blockquote> чтобы привязать группу к авторизованному аккаунту в боте",
                parseMode: ParseMode.Html,
                messageThreadId: threadId,
                cancellationToken: cancellationToken
                );
            return;
        }
        
        await SetDraft("Работа с контекстом…", ChatAction.FindLocation);
        var day = DateExtractor.GetDay(msgText, metric);
        var dayParseResult = await GetSched.GetDay(chatId, day, metric, fromGroup, ignoreEarlyDay: noHumanoidFixes);
        
        var bgWeatherTask = SchedMessageBuilder.BuildWeather(chatId, dayParseResult, isGroup, cancellationToken, metric: metric);
        
        
        await SetDraft("Парсинг токена и расписаний…", ChatAction.Typing);
        var (schedule, exams) = await GetSched.GetSchedAndExams(chatId, dayParseResult, fromGroup, metric: metric);
        var messageText = SchedMessageBuilder.BuildMessage(schedule, dayParseResult, rawExamList: exams, metric: metric);
        
        
        var keyboard = new InlineKeyboardMarkup();
        if (dayParseResult.IsEarlyDayMoveFix) {
            keyboard = new InlineKeyboardMarkup([
                [
                    new InlineKeyboardButton($"Показать на {dayParseResult.dayParsedName}")
                        { CallbackData = $"sched:To:{dayParseResult.StartDate}", Style = KeyboardButtonStyle.Danger },
                    new InlineKeyboardButton("Всё супер, закрыть")
                        { CallbackData = $"sched:C", Style = KeyboardButtonStyle.Primary }
                ]
            ]);
        }
        
        Console.WriteLine(("Before firs send:"+metric.getMetric(MetricType.Total)));
        var currentMessage = await bot.SendRichMessage(
            chatId: chatId,
            messageThreadId: threadId,
            richMessage: new InputRichMessage { Html = messageText },
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
        metric.Stop(MetricType.Total);
        await bot.SendChatAction(chatId, ChatAction.UploadDocument, threadId, cancellationToken: cancellationToken);


        var bgWeatherResult = await bgWeatherTask;
        var (finalWeather, cachedImgId) = (bgWeatherResult.Item1, bgWeatherResult.Item2);
        
        var weatherAsText = await SettingsService.GetValue(chatId, SettingsList.AllowWeather, cancellationToken) is 1;
        var useCache = cachedImgId is not null && cachedImgId.Count > 0;
        if (finalWeather.Count != 0 || useCache && cachedImgId!=null) {
            InputRichMessage newRichMessage;

            if ((isGroup && !Behaviour.Groups.AllowWeatherImageOutput) || (isPrivateChat && !Behaviour.Users.AllowWeatherImageOutput) || weatherAsText) {
                var setNewText = await SchedMessageBuilder.BuildWeatherText(chatId, dayParseResult, isGroup);
                if (setNewText is null) {
                    return;
                }

                newRichMessage = new InputRichMessage {
                    Html = messageText + setNewText
                };
            }
            else {
                newRichMessage = SchedMessageBuilder.AddWeather(messageText, bgWeatherResult);
            }
            

            currentMessage = await bot.EditMessageText(chatId, currentMessage.Id, null, richMessage: newRichMessage, cancellationToken: cancellationToken, replyMarkup: keyboard);
            
            if (!useCache) {
                await Weather.SetRichImageUrls( // that's cache system
                    finalWeather,
                    chatId,
                    dayParseResult,
                    isGroup
                );
            }
        }
        metric.StopAll();

        
        
        /* = Total,
         = Analyze,
        = Build,
        = Draft,
        = TokenParse,
        = DataParse,
        = WeatherFetch,
        = WeatherRender,*/
        var debugInfo =
            $"""
             <b> Speed Insights: </b>
             Суммарное время ответа: {metric.getMetric(MetricType.Total)}мс 
                
             Анализ контекста: {metric.getMetric(MetricType.Analyze)}мс
             Парсинг токена: {metric.getMetric(MetricType.TokenParse)}мс
             Парсинг Journal: {metric.getMetric(MetricType.DataParse)}мс
             Сборка сообщения: {Math.Round(metric.getMetric(MetricType.Build, true) / 1000f, 1)}мс ({metric.getMetric(MetricType.Build, true)}нс)
             
             Парсинг погоды: {metric.getMetric(MetricType.WeatherFetch)}мс
             Рендеринг погоды: {metric.getMetric(MetricType.WeatherRender)}мс
             Draft-Time: {metric.getMetric(MetricType.Draft)}мс
             
             <i>Триггер процент: {(calledViaContext ? args[1] : "—")}% </i>
             """;

        if (ElevatedUserConfig.DebugUID != 0)
        {
            if (isGroup && !Behaviour.Groups.AllowSendingResponseSpeed) { return; }
            if (isPrivateChat && !Behaviour.Users.AllowSendingResponseSpeed || isPrivateChat && !Behaviour.Users.AllowDisplaySpeedMetricToAnyone) { return; }

            if (isGroup) {
                await bot.SendMessage(
                    chatId: chatId,
                    text: debugInfo,
                    ParseMode.Html,
                    messageThreadId: threadId,
                    cancellationToken: cancellationToken,
                    receiverUserId: ElevatedUserConfig.DebugUID
                );
            }
            else {
                await bot.SendMessage(
                    chatId: chatId,
                    text: debugInfo,
                    ParseMode.Html,
                    cancellationToken: cancellationToken
                );
            }
        }




        
        await Task.Delay(60 * 1000, cancellationToken);
        try {
            await bot.EditMessageReplyMarkup(
                chatId: chatId,
                messageId: currentMessage.Id,
                replyMarkup: null,
                cancellationToken: cancellationToken);
        }
        catch (Exception) {
            /* content mey not change or there's no buttons since the message sent */
        }


        return;
///////////////////////////////////////////////////////////////
        async Task SetDraft(string draft, ChatAction action)
        {
            using (metric.Measure(MetricType.Draft)) {
                await draftToken.CancelAsync();
                draftToken = new CancellationTokenSource();

                if (!isPrivateChat) {
                    _ = bot.SendChatAction(chatId, action, threadId, cancellationToken: draftToken.Token);
                    return;
                }

                await bot.SendRichMessageDraft(
                    chatId: chatId,
                    draftId: uniqueDraft,
                    messageThreadId: threadId,
                    richMessage: new InputRichMessage { Html = $"<tg-thinking>{draft}</tg-thinking>" },
                    cancellationToken: draftToken.Token
                );
            }
        }
    }
}