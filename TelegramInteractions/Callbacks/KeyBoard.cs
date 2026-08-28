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
        var isAutoMsgDelete = args[0] == "autoDeleteMsg";

        if (isDelete && callbackQuery.Message is not null)
        {
            await bot.EditMessageReplyMarkup(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
        }
        
        if (isMsgDelete && callbackQuery.Message is not null)
        {
            await DeleteMsg(bot, callbackQuery, cancellationToken);
        }
        
        if (isEphMsgDelete && callbackQuery.Message is not null) {
            await DeleteEphMsg(bot, callbackQuery, whoAsking, cancellationToken);
        }
        
        if (isAutoMsgDelete && callbackQuery.Message is not null) {
            try {
                await DeleteEphMsg(bot, callbackQuery, whoAsking, cancellationToken);
            } catch (Exception e) {
                await DeleteMsg(bot, callbackQuery, cancellationToken);
            }
        }
    }



    private static async Task DeleteEphMsg(ITelegramBotClient bot, CallbackQuery callbackQuery, long whoAsking, CancellationToken cToken) {
        await bot.DeleteEphemeralMessage(
            chatId:callbackQuery.Message!.Chat.Id,
            receiverUserId: whoAsking,
            ephemeralMessageId: (int)callbackQuery.Message.EphemeralMessageId!,
            cancellationToken: cToken
        );
    }

    private static async Task DeleteMsg(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken cToken) {
        await bot.DeleteMessage(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                cancellationToken: cToken);
        
    }
}
