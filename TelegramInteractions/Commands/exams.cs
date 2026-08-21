using System.Diagnostics;
using System.Reflection.Metadata;
using JetBrains.Annotations;
using Scheder.Services.ContextDetection;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/exams")]
public class Exams : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var isGroup = ChatTools.IsGroup(message);
        var msgText = message.Text!;
        var noHumanoidFixes = msgText[^1].ToString() == "!";
        
        var day = DateExtractor.GetDay(msgText, null);
        var dayParseResult = await GetSched.GetDay(chatId, day, null, isGroup, ignoreEarlyDay: noHumanoidFixes);
        
        var exams = await GetSched.GetExamsFromApi(chatId, dayParseResult, isGroup);
        var messageText = SchedMessageBuilder.BuildExams(exams, dayParseResult, isStandalone: true, showDates: true);
        
        await bot.SendRichMessage(
            chatId: chatId,
            messageThreadId: ChatTools.GetForumId(message),
            richMessage: new InputRichMessage { Html = messageText },
            cancellationToken: cancellationToken
        );
    }
}