using System.Globalization;
using JetBrains.Annotations;
using Scheder.Services.ContextDetection;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.Services.Weather;
using Scheder.TelegramInteractions.Attributes;
using Scheder.TelegramInteractions.Commands;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Callbacks;

[UsedImplicitly]
[Callback("sched", IgnoreSplitter=false)]
public class Sched : ICallbackCommand
{
    
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        var message = callbackQuery.Message!;
        var chatId = message.Chat.Id;
        var isGroup = ChatTools.IsGroup(message);
        var isPrivateChat = ChatTools.IsPrivateChat(message);

        var actionCancel = args[0] == "C";
        var currentShownDate = actionCancel ? "" : args[1];

        if (actionCancel) {
            await bot.EditMessageReplyMarkup(
                chatId: chatId,
                messageId: message.MessageId,
                cancellationToken: cancellationToken
                );
            return;
        }

        var targetDay = DateTime
            .ParseExact(currentShownDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .AddDays(1)
            .ToString("yyyy-MM-dd");
        
        
        var day = DateExtractor.GetForcedDay(targetDay);
        var dayParseResult = await GetSched.GetForcedDay(chatId, day, null, isGroup);
        if (args[0] == "1") {dayParseResult.DayDisplay = DayType.Tomorrow;}
        if (args[0] == "2") {dayParseResult.DayDisplay = DayType.ReTomorrow;}
        var bgWeatherTask = SchedMessageBuilder.BuildWeather(chatId, dayParseResult, isGroup, cancellationToken);
        var (schedule, exams, _) = await GetSched.GetSchedAndExams(chatId, dayParseResult, isGroup);
        var messageText = SchedMessageBuilder.BuildMessage(schedule, dayParseResult, rawExamList: exams);
        
        var currentMessage = await bot.EditMessageText(
            chatId: chatId,
            messageId: message.Id,
            text: null,
            richMessage: new InputRichMessage { Html = messageText },
            cancellationToken: cancellationToken
        );
        
        var (finalWeather, cachedRichMessages) = await bgWeatherTask;
        var useCache = cachedRichMessages is not null && cachedRichMessages.Count > 0;
        if (finalWeather.Count != 0 || useCache && cachedRichMessages != null) {
            string newText;
            List<InputRichMessageMedia> mediaList = [];

            if ((isGroup && !Behaviour.Groups.AllowWeatherImageOutput) || (isPrivateChat && !Behaviour.Users.AllowWeatherImageOutput)) {
                var setNewText = await SchedMessageBuilder.BuildWeatherText(chatId, dayParseResult, isGroup);
                if (setNewText is null) {
                    return;
                }
                newText = setNewText;
            }
            else {
                newText = $"""
                           <h5> Погода: </h5> 
                           <tg-slideshow>
                          """;

                
                if (!useCache) {
                    var i = -1;
                    foreach (var weather in finalWeather) {
                        i++;
                        var stream = new MemoryStream(weather);
                        mediaList.Add(
                            new InputRichMessageMedia
                            {
                                Id = $"w{i + 1}",
                                Media = new InputMediaPhoto(stream)
                            }
                        );
                    }
                    
                }
                else {
                    mediaList.AddRange(cachedRichMessages!);
                }
            }
            
            for (var i = 0; i < mediaList.Count; i++)
            {
                newText += $"""<img src="tg://photo?id=w{i + 1}">""";
            }
            newText += "</tg-slideshow>";

            messageText += newText;
            var irm = new InputRichMessage {
                Html = messageText,
                Media = mediaList
            };

            await bot.EditMessageText(chatId, currentMessage.Id, null, richMessage: irm, cancellationToken: cancellationToken);
            
            if (!useCache) {
                await Weather.SetRichImageUrls(
                    finalWeather,
                    chatId,
                    dayParseResult,
                    isGroup
                );
            }
        }
    }


}