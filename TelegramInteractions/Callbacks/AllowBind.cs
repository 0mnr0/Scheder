using Scheder.Attributes;
using Scheder.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Scheder.Callbacks;

[Callback("allowbind", IgnoreSplitter=false)]
public class AllowBind : ICallbackCommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        var message = callbackQuery.Message;
        var group = Convert.ToInt64(args[0]);
        var token = args[1];
        var isAllowed = args[2] != "cancel";
        var isCanceling = args[2] == "cancel";
        var origMessage = (isAllowed) ? args[3] : null;
        
        if (message == null)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Failed to delete message: code 101;",
                cancellationToken: cancellationToken
            );
            return;
        }
        long? chatId = message.Chat.Id;



        if (isCanceling)
        {
            await bot.DeleteMessage(message.Chat.Id, message.Id, cancellationToken);
            return;
        }

        if (await Memory.Group.getGroupBindToken(group) != token)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Ключ безопасности не подошёл, попробуйте заново;",
                cancellationToken: cancellationToken
            );
            return;
        }
        
        
        await bot.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Ваши данные привязаны к группе!",
            cancellationToken: cancellationToken
        );
        await Memory.Group.setGroupBindToken(group, string.Empty);
        await Memory.Group.setGroupBind(group, callbackQuery.From.Id);
        await Memory.User.LinkGroup(callbackQuery.From.Id, group);
        
        
        try
        {
            
            await bot.DeleteMessage(chatId, message.Id, cancellationToken);
            if (origMessage != null)
            {
                
                Console.WriteLine("chatId:  " + chatId + " | receiverUserId:" + callbackQuery.From.Id + " | emid: "+Convert.ToInt32(origMessage));
                await bot.EditEphemeralMessageText(
                    chatId: chatId,
                    receiverUserId: callbackQuery.From.Id,
                    ephemeralMessageId: Convert.ToInt32(origMessage),
                    "<b> Группа привязана к вам! </b> \n\n Все последующие запросы будут идти приоритетно через ваши данные",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                await bot.EditEphemeralMessageReplyMarkup(
                    chatId: chatId,
                    receiverUserId: callbackQuery.From.Id,
                    ephemeralMessageId: Convert.ToInt32(origMessage),
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000, cancellationToken);
                await bot.DeleteEphemeralMessage(
                    chatId: chatId,
                    receiverUserId: callbackQuery.From.Id,
                    ephemeralMessageId: Convert.ToInt32(origMessage),
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
