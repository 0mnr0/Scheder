using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Callbacks;

[Callback("keyboard", IgnoreSplitter=false)]
public class KeyBoard : ICallbackCommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = callbackQuery.From.Id;
        var isDelete = args[0] == "remove";
        var isMsgDelete = args[0] == "deleteMsg";
        var isEphMsgDelete = args[0] == "deleteEphMsg";

        if (isDelete && callbackQuery.Message is not null)
        {
            await bot.EditMessageReplyMarkup(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
        }
        
        if (isMsgDelete && callbackQuery.Message is not null)
        {
            await bot.DeleteMessage(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
        }
        
        if (isEphMsgDelete && callbackQuery.Message is not null)
        {
            await bot.DeleteEphemeralMessage(
                chatId:callbackQuery.Message.Chat.Id,
                receiverUserId: whoAsking,
                ephemeralMessageId: (int)callbackQuery.Message.EphemeralMessageId!,
                cancellationToken: cancellationToken
                );
        }


    }
}
