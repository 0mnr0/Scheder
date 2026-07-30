using System.Globalization;
using Scheder.Commands;
using Scheder.Config;
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


[Callback("sched", IgnoreSplitter=false)]
public class sched : ICallbackCommand
{
    private readonly Sched _schedCommand = new();
    
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = callbackQuery.From.Id;
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
        var dayParseResult = await GetSched.GetForcedDay(chatId, day, isGroup);
        if (args[0] == "1") {dayParseResult.dayDisplay = DayType.Tomorrow;}
        if (args[0] == "2") {dayParseResult.dayDisplay = DayType.ReTomorrow;}
        var bgWeatherTask = SchedMessageBuilder.BuildWeather(chatId, dayParseResult, isGroup, cancellationToken);
        var (schedule, exams) = await GetSched.GetSchedAndExams(chatId, dayParseResult, isGroup);
        var messageText = SchedMessageBuilder.BuildMessage(schedule, dayParseResult, rawExamList: exams);
        
        var currentMessage = await bot.EditMessageText(
            chatId: chatId,
            messageId: message.Id,
            text: null,
            richMessage: new InputRichMessage { Html = messageText },
            cancellationToken: cancellationToken
        );
        
        var (finalWeather, cachedImgId) = await bgWeatherTask;
        var useCache = cachedImgId is not null && cachedImgId.Count > 0;
        if (finalWeather.Count != 0 || useCache && cachedImgId != null) {
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
                Console.WriteLine("useCache: "+useCache);
                newText = $"""
                           <h5> Погода: </h5> 
                           <tg-slideshow>
                               <img src="tg://photo?id=w1">
                               <img src="tg://photo?id=w2">
                           </tg-slideshow>
                          """;

                
                if (!useCache) {
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
                    mediaList.AddRange(cachedImgId!);
                }
            }

            messageText += newText;
            var irm = new InputRichMessage {
                Html = messageText,
                Media = mediaList
            };

            currentMessage = await bot.EditMessageText(chatId, currentMessage.Id, null, richMessage: irm, cancellationToken: cancellationToken);
            
            if (!useCache) {
                await Weather.SetRichImageUrls(
                    finalWeather,
                    chatId,
                    dayParseResult,
                    isGroup
                );
            }
        }



        //await _schedCommand.ExecuteAsync(bot, message, ["forceDate", targetDay], cancellationToken);
    }


}