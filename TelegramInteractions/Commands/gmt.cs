using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Commands;

[Command("/gmt")]
public class gmt : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var msg = message;
        
        
        if (!ChatTools.IsPrivateChat(message)) {
            await bot.SendMessage(msg.Chat.Id, "Эта функция настраивается в личных сообщениях тем, кто привязал группу.", cancellationToken: cancellationToken);
            return;
        }


        /*
        var txt =
            $"""

             <table bordered valign>
                 <tr><th> Какое у вас время? </th></tr>
                 <tr align="center"> <td valign> 00:{currentMinutes} </td> </tr>
             </table>

             """;

        
        

        await bot.SendRichMessage(
            msg.Chat.Id,
            new InputRichMessage
            {
                Html = txt
            },
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);*/
        
        
        var currentMinutes = DateTime.Now.ToString("mm");
        var keyboard = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("-1 Час", $"gmt:00:-"),
                InlineKeyboardButton.WithCallbackData("Синхр", $"gmt:00:sync"),
                InlineKeyboardButton.WithCallbackData("+1 Час", $"gmt:00:+")
            ],
            [
                new InlineKeyboardButton("Сохранить") {CallbackData = $"gmt:00:save", Style = KeyboardButtonStyle.Success},
                new InlineKeyboardButton("Отмена") {CallbackData = $"gmt:00:cancel", Style = KeyboardButtonStyle.Danger}
            ]
        ]);
        await bot.SendMessage(
            msg.Chat.Id,
            MDTools.EscapeMarkdownV2($"<b>Какое у вас время?</b>\n00:{currentMinutes}"),
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
        
    }
}