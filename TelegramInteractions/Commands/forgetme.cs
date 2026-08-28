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
        var isGroup = ChatTools.IsGroup(message);
        var threadId = ChatTools.GetForumId(message);
        
        if (isGroup) {
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Эта команда доступна только в личных чатах",
                messageThreadId: threadId,
                cancellationToken: cancellationToken
            );
            
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