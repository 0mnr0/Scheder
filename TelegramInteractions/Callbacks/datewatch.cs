using Scheder.Services.DateWatcher;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Callbacks;


[Callback("datewatch", IgnoreSplitter=false)]
public class datewatch : ICallbackCommand {
    
    public async Task ExecuteAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, string[] args,
        CancellationToken cancellationToken) {
        
        var msg = callbackQuery.Message!;
        var chatId = msg.Chat.Id;
        var isGroup = ChatTools.IsGroup(msg);
        var whoAsked = msg.From!.Id;
        var isAdmin = await ChatTools.IsUserAdmin(bot, chatId, whoAsked);
        var action = args[0]; // [RM - Remove, CM - Confirm]
        var targetDate = args[1];
        var isUserNotAllowed = isGroup && !Behaviour.Groups.AllowNonAdminsToDateWatch && !isAdmin;

        if (isGroup && !isUserNotAllowed) {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Вы должны быть администратором в группе для выполнения этой команды!",
                cancellationToken: cancellationToken
            );
            return;
        } 
        
        switch (action) {
            case "cm": {
                await bot.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    cancellationToken: cancellationToken
                );
            
                var keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton> {
                    new("Удалить!")
                        { CallbackData = $"datewatch:rm:{targetDate}", Style = KeyboardButtonStyle.Danger },
                    new("Оставить")
                        { CallbackData = $"keyboard:{(isGroup ? "deleteEphMsg" : "deleteMsg")}", Style = KeyboardButtonStyle.Success },
                });
            
                await bot.SendMessage(
                    chatId: chatId,
                    $"Вы действительно хотите удалить прослушиватель на дату {targetDate}?",
                    receiverUserId: isGroup ? whoAsked : null,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
                return;
            }
            
            
            case "rm": {
                IEntitySource memorySource = isGroup ? new GroupSource() : new UserSource();
                await memorySource.RemoveDayListener(chatId, targetDate);

                await bot.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id, 
                    $"Изменения на {targetDate} больше приходить не будут",
                    cancellationToken: cancellationToken
                );

                try {
                    if (isGroup) {
                        if (callbackQuery.Message != null)
                            await bot.DeleteEphemeralMessage(
                                chatId: chatId,
                                whoAsked,
                                (int)callbackQuery.Message.EphemeralMessageId!,
                                cancellationToken: cancellationToken
                            );
                    }
                    else {
                        await bot.DeleteMessage(
                            chatId: chatId,
                            messageId: callbackQuery.Message!.Id,
                            cancellationToken: cancellationToken);
                    }
                }
                catch (Exception) {
                    // ignored
                }

                return;
            }
        }
    }
}