using Scheder.Attributes;
using Scheder.JournalAPI;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Scheder.Tools.RawTelegramApi;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Scheder.Tools.MDTools;

namespace Scheder.Commands;


[Command("/auth")]
public class auth : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("//auth "+args);
        var whoAsking = message.From?.Id;
        var msgTxt = message.Text;
        Console.WriteLine(msgTxt);
        if (whoAsking == null || msgTxt == null)
        {
            return;
        }

        var wasRegistered = await Memory.User.IsUserExistsAsync((long) whoAsking);
        var isGroup = ChatTools.IsGroup(message);
        var authData = msgTxt.Replace("/auth", "").Replace("  ", " ").Replace(" ", "").Split(",");

        
        if (isGroup) {
            const string alertText = "Ни в коем случае больше так не делайте. Не выполняйте авторизацию в группах, так вы можете показать ваш пароль и логин всем участникам группы. Благо, эта команда видна только вам. Удалите сообщение";
            await bot.SendMessage(
                message.Chat.Id,
                alertText,
                messageThreadId: ChatTools.GetForumId(message),
                parseMode: ParseMode.Html,
                receiverUserId: whoAsking,
                cancellationToken: cancellationToken
            );
            
            return;
        }
        
        
        if (authData.Length != 2) {
            await bot.SendMessage(
                message.Chat.Id,
                "Неправильное количество аргументов, пожалуйста, используйте следующий синтаксис:\n\n<blockquote> <b>/auth login, password</b> </blockquote>\nЗапятая разделяет логин и пароль, пробелы игнорируются.",
                messageThreadId: ChatTools.GetForumId(message),
                parseMode: ParseMode.Html,
                receiverUserId: whoAsking,
                cancellationToken: cancellationToken
            );
            return;
        }
        

        // add ephemeral message to change user password if he sent it to the group.
        
        
        var msg = await bot.SendMessage(
            chatId: message.Chat.Id,
            EscapeMarkdownV2("Дайте пару секунд, нужно проверить логин и пароль..."),
            parseMode: ParseMode.MarkdownV2,
            messageThreadId: ChatTools.GetForumId(message),
            cancellationToken: cancellationToken);
        
        
        
        
        
        
        var payload = await API.GetAuthAsync(authData[0], authData[1]);
        if (payload == null)
        {
            await bot.EditMessageText(
                message.Chat.Id,
                msg.Id,
                "Не удалось вас авторизовать, проверьте данные от входа и попробуйте ещё раз", cancellationToken: cancellationToken);
        }
        else
        {
            if (!wasRegistered)
            {
                await Memory.User.RegisterAsync((long) whoAsking);
            }
            
            await Memory.User.SaveAuthAsync(
                (long) whoAsking,
                authData[0],
                authData[1]
            );
            
            await Memory.User.SetJWTAsync(
                (long) whoAsking,
                payload["access_token"]!.ToString()
            );
            
            await Memory.User.SetCity(
                (long) whoAsking,
                payload["city_data"]?["timezone_name"]?.ToString()
            );
            
            await bot.EditMessageText(
                message.Chat.Id,
                msg.Id, 
                ("Супер! Теперь вы зарегистрированы в боте, можете пользоваться всеми доступными командами"), cancellationToken: cancellationToken);
            
        }

    }
}