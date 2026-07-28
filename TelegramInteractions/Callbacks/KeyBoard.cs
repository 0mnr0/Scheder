using Scheder.Attributes;
using Scheder.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.Callbacks;

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

        if (isDelete)
        {
            await bot.EditMessageReplyMarkup(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
        }
        
        if (isMsgDelete)
        {
            await bot.DeleteMessage(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
        }
        
        if (isEphMsgDelete)
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
