using System.Diagnostics;
using Scheder.Config;
using Scheder.ContextDetection;
using Scheder.JournalAPI;
using Scheder.Services.WebRender;
using Scheder.Tools;
using Scheder.Tools.RawTelegramApi;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types.Enums;

namespace Scheder.Commands;

using Telegram.Bot;
using Telegram.Bot.Types;
using Attributes;

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
        var stopwatch = Stopwatch.StartNew();
        var chatId = message.Chat.Id;
        var msgText = message.Text;
        var fromGroup = !ChatTools.IsPrivateChat(message);
        var isGroup = ChatTools.IsGroup(message);
        var isPrivateChat = !isGroup;
        var uniqueDraft = (int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var threadId = ChatTools.GetForumId(message);
        var noHumanoidFixes = msgText[^1].ToString() == "!";
        var calledViaContext = args.Length > 0;

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

        await SetDraft("Работа с контекстом…");
        var day = DateExtractor.GetDay(msgText);
        var dayParseResult = await GetSched.GetDay(chatId, day, fromGroup, ignoreEarlyDay: noHumanoidFixes);
        var bgWeatherTask = SchedMessageBuilder.BuildWeather(chatId, dayParseResult, isGroup);
        
        await SetDraft("Парсинг токена и расписаний…");
        var (schedule, exams) = await GetSched.GetSchedAndExams(chatId, dayParseResult, fromGroup);
        var messageText = SchedMessageBuilder.BuildMessage(schedule, dayParseResult, rawExamList: exams);
        
        var compileTime = stopwatch.Elapsed.Milliseconds;
        var sentMessage = await bot.SendRichMessage(
            chatId: chatId,
            messageThreadId: threadId,
            richMessage: new InputRichMessage { Html = messageText },
            cancellationToken: cancellationToken
        );


        var (finalWeather, cachedImgId) = await bgWeatherTask;
        var currentMessage = sentMessage;

        var useCache = cachedImgId is not null && cachedImgId.Count > 0;
        if (finalWeather.Count != 0 || useCache && cachedImgId!=null) {
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

            currentMessage = await bot.EditMessageText(chatId, sentMessage.Id, null, richMessage: irm, cancellationToken: cancellationToken);
            
            if (!useCache) {
                await Weather.SetRichImageUrls(
                    finalWeather,
                    chatId,
                    dayParseResult,
                    isGroup
                );
            }
        }

        var debugInfo =
            $"""
             <i>Суммарное время ответа: {stopwatch.Elapsed.Milliseconds}мс | Сборка сообщения: {compileTime}мс </i>
             <i>Триггер процент: {(calledViaContext ? args[0] : "false")} </i>
             """;

        if (fromGroup && ElevatedUserConfig.DebugUID != 0)
        {
            if (isGroup && !Behaviour.Groups.AllowSendingResponseSpeed) { return; }
            await bot.SendMessage(
                chatId: chatId,
                text: debugInfo,
                ParseMode.Html,
                messageThreadId: threadId,
                cancellationToken: cancellationToken,
                receiverUserId: ElevatedUserConfig.DebugUID
            );
        }
        else if (message.From != null && message.From.Id == ElevatedUserConfig.DebugUID && ElevatedUserConfig.DebugUID != 0) {
            if (isPrivateChat && !Behaviour.Users.AllowSendingResponseSpeed) { return; }
            
            var debugRichMessage = currentMessage.ToInputRichMessage();
            debugRichMessage!.Html += "<br>" + debugInfo;

            currentMessage = await bot.EditMessageText(
                chatId: chatId,
                messageId: sentMessage.MessageId,
                "",
                richMessage: debugRichMessage,
                cancellationToken: cancellationToken
            );
        }
        stopwatch.Stop();
        return;
        
        
///////////////////////////////////////////////////////////////
        async Task SetDraft(string draft)
        {
            if (!ChatTools.IsPrivateChat(message)) return;
            
            await bot.SendRichMessageDraft(
                chatId: chatId,
                draftId: uniqueDraft,
                messageThreadId: threadId,
                richMessage: new InputRichMessage { Html = $"<tg-thinking>{draft}</tg-thinking>" },
                cancellationToken: cancellationToken
            );
        }
    }
}