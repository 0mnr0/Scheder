using JetBrains.Annotations;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/forgetme")]
public class Forgetme : ICommand{
    
    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, string[] args, CancellationToken cancellationToken) {
        if (message.From is null) return;
        
        var isGroup = ChatTools.IsGroup(message);
        var threadId = ChatTools.GetForumId(message);
        
        if (isGroup) {
            var eMsg = await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Эта команда доступна только в личных чатах",
                ephemeralMessageParameters: new EphemeralMessageParameters {ReceiverUserId = message.From.Id},
                messageThreadId: threadId,
                cancellationToken: cancellationToken
            );
            
            await Task.Delay(5000, cancellationToken);
            
            await bot.DeleteEphemeralMessage(eMsg.Chat.Id, message.From.Id, (int)eMsg.EphemeralMessageId!, cancellationToken);
            await bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
            
            return;
        }


        await bot.SendRichMessage(
            chatId: message.Chat.Id,
            richMessage: new InputRichMessage {
                Html = """
                       <h4> Стереть данные? </h4>
                       <p> В таком случае будут отвязаны все группы и стерты все записи о вас из базы данных. Учтите что кэш аутентификационных данных может быть доступен ещё некоторое время </p>
                       <br>
                       
                       <tg-button-row>
                         <tg-button type="callback_data" data="forgetme:confirm">Стереть</tg-button>
                         <tg-button type="callback_data" data="keyboard:deleteMsg" style="primary">Отмена</tg-button>
                       </tg-button-row>
                       """
            },
            cancellationToken: cancellationToken
        );




    }
}