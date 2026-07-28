using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Callbacks;


[Callback("unbound", IgnoreSplitter=false)]
public class Unbound : ICallbackCommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("args: "+ string.Join(" ", args));
        var whoAsking = callbackQuery.From.Id;
        var targetChatId = long.Parse(args[0]);
        var isHardUnbound = args.Length > 1 && args[1] == "hard";
        var isUserAdmin = await ChatTools.IsUserAdmin(bot, targetChatId, whoAsking);
        if (!isUserAdmin)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Вы не являетесь администратором в чате.",
                cancellationToken: cancellationToken
            );
            return;
        }
        Console.WriteLine("2");
        
        var boundedTo = await Memory.Group.getGroupBind(targetChatId);
        var isUserBoundedWithTargetGroup = (await Memory.User.GetLinkedGroups(whoAsking)).Contains(targetChatId);
        
        if (isHardUnbound)
        {
            await Memory.Group.setGroupBind(targetChatId, null);
            if (boundedTo != null)
            {
                await Memory.User.UnlinkGroup((long)boundedTo, targetChatId);
            }
            
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Группа отвязана.",
                cancellationToken: cancellationToken
            );
            
            
            await bot.DeleteEphemeralMessage(
                chatId: targetChatId,
                receiverUserId: whoAsking,
                ephemeralMessageId: (int)callbackQuery.Message!.EphemeralMessageId!,
                cancellationToken: cancellationToken
                );
            return;
        }
        
        
        if (boundedTo == null && isUserBoundedWithTargetGroup)
        {
            await Memory.User.UnlinkGroup(whoAsking, targetChatId);
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Вы были отвязаны от группы.",
                cancellationToken: cancellationToken
            );
            return;
        }
        
        if (boundedTo != whoAsking)
        {
            await Memory.User.UnlinkGroup(whoAsking, targetChatId);
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Запись привязке к этой группе была неверна. Тем не менее - группа отвязана",
                cancellationToken: cancellationToken
            );
            return;
        }
        
        Console.WriteLine($"{boundedTo == whoAsking} | {isUserBoundedWithTargetGroup}");
        if (boundedTo == whoAsking && isUserBoundedWithTargetGroup)
        {
            await Memory.User.UnlinkGroup(whoAsking, targetChatId);
            await Memory.Group.setGroupBind(targetChatId, null);
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Привязка отозвана",
                cancellationToken: cancellationToken
            );
            try
            {
                
                var deBoundKeyBoard = new InlineKeyboardMarkup([
                    [
                        new InlineKeyboardButton("Закрыть") {CallbackData = $"keyboard:deleteEphMsg", Style = KeyboardButtonStyle.Primary }
                    ]
                ]);
                
                
                await bot.SendMessage(
                    chatId: targetChatId,
                    "Вы больше не являетесь главным поставщиком аутентификационных данных. Чтобы привязать группу используйте \"/bindgroup.\"",
                    receiverUserId: whoAsking,
                    replyMarkup: deBoundKeyBoard,
                    cancellationToken: cancellationToken);

                
            }
            catch (Exception)
            {
                // ignored
            }
            
            await UpdateMarkup(bot, whoAsking, callbackQuery, cancellationToken);
        }
        
        
    }


    private static async Task UpdateMarkup(ITelegramBotClient bot, long whoAsking, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var keyboard = await CodeBunch.GetGroupList(bot, whoAsking, cancellationToken);
        
        await bot.EditMessageReplyMarkup(
            chatId: whoAsking,
            messageId: callbackQuery.Message!.MessageId,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}