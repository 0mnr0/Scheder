using System.Diagnostics;
using System.Reflection.Metadata;
using Scheder.Services.ContextDetection;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Commands;


[Command("/exams")]
public class exams : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoTriggered = message.From!.Id;
        var chatId = message.Chat.Id;
        var isGroup = ChatTools.IsGroup(message);
        var msgText = message.Text!;
        var noHumanoidFixes = msgText[^1].ToString() == "!";
        
        var day = DateExtractor.GetDay(msgText);
        var dayParseResult = await GetSched.GetDay(chatId, day, isGroup, ignoreEarlyDay: noHumanoidFixes);
        
        var weatherTimer = Stopwatch.StartNew();
        var bgWeatherTask = SchedMessageBuilder.BuildWeather(chatId, dayParseResult, isGroup, weatherTimer);
        
        var exams = await GetSched.GetExamsFromApi(chatId, dayParseResult, isGroup);
        
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

        var messageText = SchedMessageBuilder.BuildExams(exams, dayParseResult, isStandalone: true, showDates: true);
        var weather = await bgWeatherTask;
        
        var currentMessage = await bot.SendRichMessage(
            chatId: chatId,
            messageThreadId: ChatTools.GetForumId(message),
            richMessage: new InputRichMessage { Html = messageText },
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}