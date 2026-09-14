using JetBrains.Annotations;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands;


[UsedImplicitly]
[Command("/timemiss")]
public class Timemiss : ICommand {

    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken) {

        var editMessage = args.Length > 0 && args[0] == "msgChange";
        var editInDirect = args.Length > 1 && args[1] == "0";
        long? ephemeralEditFor = args.Length > 2 ? long.Parse(args[2]) : null;
        var isPrivateChat = ChatTools.IsPrivateChat(message);
        
        if (Env.TimeMissApi is null) {
            await bot.SendRichMessage(
                message.Chat.Id,
                richMessage: new InputRichMessage {
                    Html = "<h3> Функция отключена в настройках окружения </h3>" +
                           "<tg-button type=\"callback_data\" data=\"keyboard:autoDeleteMsg\">Понятно</tg-button>"
                },
                messageThreadId: message.MessageThreadId,
                ephemeralMessageParameters: isPrivateChat
                    ? null
                    : new EphemeralMessageParameters { ReceiverUserId = message.From!.Id },
                cancellationToken: cancellationToken
            );
            return;
        }

        var msgContent = new InputRichMessage {
            Html = """
                   <h4> Уведомление для преподавателя </h4>
                   <p> Работает только для первой пары. Не гарантируется что преподаватель сможет увидеть сообщение об опоздании </p>
                   <details>
                        <summary>Подробнее</summary>
                        <blockquote> Вы должны пройти авторизацию в боте для корректной работы этой функции (а как ещё узнать ваше ФИО). Преподаватель должен иметь расширение Omni Tools (1.7.0+) для поддержки этой функции. Отметка об опоздании будет удалена по истечении 4х часов. </blockquote>
                   </details>

                   <br>
                     <tg-button-row>
                        <tg-button type="callback_data" data="timemiss:w15" style="primary">Опоздаю (до 15мин)</tg-button>
                     </tg-button-row>
                     <tg-button-row>
                        <tg-button type="callback_data" data="timemiss:w30" style="primary">Опоздаю (до 30мин)</tg-button>
                     </tg-button-row>
                     <tg-button-row>
                        <tg-button type="callback_data" data="timemiss:unk" style="primary">Приду в течении пары</tg-button>
                     </tg-button-row>
                     
                     <p>ㅤ</p>
                     </hr>
                     
                     <tg-button-row>
                        <tg-button type="callback_data" data="timemiss:rem" style="primary">Убрать запись</tg-button>
                        <tg-button type="callback_data" data="keyboard:autoDeleteMsg" style="danger">Закрыть</tg-button>
                     </tg-button-row>
                   """
        };


        if (!editMessage) {
            await bot.SendRichMessage(
                chatId: message.Chat.Id,
                richMessage: msgContent,
                messageThreadId: message.MessageThreadId,
                ephemeralMessageParameters: isPrivateChat
                    ? null
                    : new EphemeralMessageParameters { ReceiverUserId = message.From!.Id },
                cancellationToken: cancellationToken
            );
        }
        else {
            if (editInDirect) {
                await bot.EditMessageText(
                    message.Chat.Id,
                    message.MessageId,
                    null,
                    richMessage: msgContent,
                    cancellationToken: cancellationToken);
            }
            else if (ephemeralEditFor is not null) {
                await bot.EditEphemeralMessageText(
                    message.Chat.Id,
                    (long)ephemeralEditFor,
                    (int)message.EphemeralMessageId!,
                    null,
                    richMessage: msgContent,
                    cancellationToken: cancellationToken
                );
            }
        }

    }
}