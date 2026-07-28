using Scheder.Attributes;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.Commands;


[Command("/unbound")]
public class unbound : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoTriggered = message.From!.Id;
        var chatId = message.Chat.Id;
        var isGroup = ChatTools.IsGroup(message);

        if (!isGroup)
        {
            var keyboard = await CodeBunch.GetGroupList(bot, chatId, cancellationToken);
            
            
            await bot.SendMessage(
                chatId,
                "Выберите из списка групп ту, которую надо отключить от вашей авторизации:\n\n<i>Если нужной в списке нет - вы можете выполнить команду в нужной группе. </i>",
                replyMarkup: keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        else
        {


            var isBoundedTo = await Memory.Group.getGroupBind(chatId);
            if (isBoundedTo == whoTriggered)
            {
                var keyboard = new InlineKeyboardMarkup([
                    [
                        new InlineKeyboardButton("Отвязать группу")
                            { CallbackData = $"unbound:{chatId}:hard", Style = KeyboardButtonStyle.Danger }
                    ],
                    [
                        new InlineKeyboardButton("Отмена") 
                            { CallbackData = "keyboard:deleteEphMsg", Style = KeyboardButtonStyle.Primary }
                    ]
                ]);
                
                await bot.SendMessage(
                    chatId,
                    "Вы точно хотите отвязать эту группу от себя?",
                    replyMarkup: keyboard,
                    parseMode: ParseMode.Html,
                    receiverUserId: whoTriggered,
                    cancellationToken: cancellationToken
                );
            } else if (await ChatTools.IsUserAdmin(bot, chatId, whoTriggered))
            {
                var keyboard = new InlineKeyboardMarkup([
                    [
                        new InlineKeyboardButton("Отвязать группу")
                            { CallbackData = $"unbound:{chatId}:hard", Style = KeyboardButtonStyle.Danger }
                    ],
                    [
                        new InlineKeyboardButton("Отмена") 
                            { CallbackData = "keyboard:deleteEphMsg", Style = KeyboardButtonStyle.Primary }
                    ]
                ]);
                
                await bot.SendMessage(
                    chatId,
                    "Вы точно хотите отвязать эту группу? Используется не ваши аутентификационные данные.",
                    replyMarkup: keyboard,
                    parseMode: ParseMode.Html,
                    receiverUserId: whoTriggered,
                    cancellationToken: cancellationToken
                );
            }

        } 
    }
}