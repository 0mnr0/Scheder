using JetBrains.Annotations;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Scheder.Tools.MDTools;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/auth")]
public class Auth : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = message.From?.Id;
        var msgTxt = message.Text;
        if (whoAsking == null || msgTxt == null)
        {
            return;
        }

        var wasRegistered = await Memory.User.IsUserExistsAsync((long) whoAsking);
        var isGroup = ChatTools.IsGroup(message);
        var authData = msgTxt.Replace("/auth", "").Replace("  ", " ").Replace(" ", "").Split(",");

        
        if (isGroup) {
            const string alertTitle =  "<h4>Ни в коем случае больше так не делайте </h4>\n";
            const string alertText = 
                "<p>Не выполняйте авторизацию в группах, так вы можете показать ваш пароль и логин всем участникам группы</p>" ;
            
            var alertMsg = await bot.SendRichMessage(
                message.Chat.Id,
                richMessage:  new InputRichMessage { Html = alertTitle + alertText + "\n<p> Мы пытаемся удалить это сообщение... </p>" },
                messageThreadId: ChatTools.GetForumId(message),
                ephemeralMessageParameters: new EphemeralMessageParameters {ReceiverUserId = (long)whoAsking},
                cancellationToken: cancellationToken
            );

            try {
                await bot.DeleteMessage(
                    chatId: message.Chat.Id,
                    messageId: message.Id,
                    cancellationToken: cancellationToken
                );
                
                await bot.EditEphemeralMessageText(
                    alertMsg.Chat.Id,
                    (long)whoAsking,
                    (int)alertMsg.EphemeralMessageId!,
                    null,
                    richMessage: new InputRichMessage {
                        Html = alertTitle + alertText +"<p> Настоятельно рекомендуется сменить пароль </p>"
                    },
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception) {
                await bot.EditEphemeralMessageText(
                    alertMsg.Chat.Id,
                    (long)whoAsking,
                    (int)alertMsg.EphemeralMessageId!,
                    null,
                    richMessage: new InputRichMessage {
                        Html = "<h1> Удалите ваше сообщение! </h1> \n <h2> У бота нет прав на удаление сообщений! </h2>"
                               + alertText
                    },
                    cancellationToken: cancellationToken
                );
            }
            
            return;
        }
        
        
        if (authData.Length != 2) {
            await bot.SendRichMessage(
                message.Chat.Id,
                richMessage: new InputRichMessage {
                    Html = "<h3> Неправильное количество аргументов </h3>"
                            + "<p> Пожалуйста, используйте следующий синтаксис: </p> \n<br> </br>" 
                            + "<blockquote> <b>/auth login, password</b> </blockquote>\n<br> </br>"
                            + "Запятая разделяет логин и пароль, пробелы стираются"
                },
                messageThreadId: ChatTools.GetForumId(message),
                ephemeralMessageParameters: isGroup ? new EphemeralMessageParameters { ReceiverUserId = (long)whoAsking } : null,
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
        
        
        
        
        
        
        var (payload, authRes) = await API.GetAuthAsync(authData[0], authData[1]);
        if (payload == null)
        {
            await bot.EditMessageText(
                message.Chat.Id,
                msg.Id,
                null,
                richMessage: new InputRichMessage {
                    Html = $"""
                                <h4> Неудача :( </h4>
                                <p> Не удалось вас авторизовать, проверьте данные от входа и попробуйте ещё раз </p>
                                <p>ㅤ</p>
                                <details>
                                    <summary>Подробнее</summary>
                                    <p> Код ошибок: {string.Join(", ",  authRes)} </p>
                                    <hr/>
                                    <i>Иногда сервера Top Academy не отвечают успешной авторизацией даже при правильных аутентификационных данных, попробуйте позже. </i>
                                </details>
                           """
                },
                cancellationToken: cancellationToken);
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
            
            await Memory.User.SetJwtAsync(
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
                null,
                richMessage: new InputRichMessage {
                    Html = "<h3>Супер!</h3>\n<p>Теперь вы зарегистрированы в боте, можете пользоваться всеми доступными командами</p>"
                },
                cancellationToken: cancellationToken);
            
        }

    }
}