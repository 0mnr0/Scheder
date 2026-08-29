using JetBrains.Annotations;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/gmt")]
public class Gmt : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        if (message.From is null) return;
        
        if (!ChatTools.IsPrivateChat(message)) {
            var msg = await bot.SendMessage(
                message.Chat.Id, "Эта функция настраивается в личных сообщениях того, кто привязал группу.",
                ephemeralMessageParameters: new EphemeralMessageParameters {ReceiverUserId = message.From.Id},
                messageThreadId: ChatTools.GetForumId(message),
                cancellationToken: cancellationToken
                );
            
            await Task.Delay(5000, cancellationToken);
            await bot.DeleteEphemeralMessage(msg.Chat.Id, message.From.Id, (int)msg.EphemeralMessageId!, cancellationToken);
            await bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
            
            return;
        }

        var currentHour = DateTime.Now.Hour;
        var currentMinutes = DateTime.Now.ToString("mm");

        // 
        var txt =
            $"""
             <h3> Какое время у вас? </h3>
             <table bordered striped>
                 <thead>
                     <tr><th align="center"> <h4> {currentHour}:{currentMinutes} </h4> </th></tr>
                 </thead>
             </table>
             """;

        
        
        var keyboard = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("-1 Час", $"gmt:{currentHour}:-"),
                InlineKeyboardButton.WithCallbackData("Синхр", $"gmt:{currentHour}:sync"),
                InlineKeyboardButton.WithCallbackData("+1 Час", $"gmt:{currentHour}:+")
            ],
            [
                new InlineKeyboardButton("Сохранить") {CallbackData = $"gmt:{currentHour}:save", Style = KeyboardButtonStyle.Success},
                new InlineKeyboardButton("Отмена") {CallbackData = $"gmt:{currentHour}:cancel", Style = KeyboardButtonStyle.Danger}
            ]
        ]);
        
        await bot.SendRichMessage(
            message.Chat.Id,
            new InputRichMessage
            {
                Html = txt
            },
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
        
        
        /*await bot.SendMessage(
            message.Chat.Id,
            MDTools.EscapeMarkdownV2($"<b>Какое у вас время?</b>\n00:{currentMinutes}"),
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);*/
        
    }
}