using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Scheder.Tools.MDTools;

namespace Scheder.TelegramInteractions.Commands;

[Command("/bindgroup")]
public class bindgroup : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = message.From?.Id;
        var chatId =  message.Chat.Id;
        var isAdminAsking = await ChatTools.IsUserAdmin(bot, chatId, whoAsking);
        var threadId = ChatTools.GetForumId(message);
        if (whoAsking == null)
        {
            return;
        }
        var isAlreadyBound = await Memory.Group.IsGroupBind(chatId);
        if (isAlreadyBound)
        {
            var boundFor = await Memory.Group.getGroupBind(chatId);
            var unboundAsAdmin = isAdminAsking && boundFor != whoAsking;
            List<InlineKeyboardButton> unboundButton =
            [
                // Проверить отвязку от имени другого админа (к которому нет привязки)
                new($"Отвязать группу {(unboundAsAdmin ? " (Как админ)" : "")}")
                    { CallbackData = $"unbound:{chatId}{(unboundAsAdmin ? ":hard" : "")}", Style = KeyboardButtonStyle.Danger }
            ];
            
            var deBoundKeyBoard = new InlineKeyboardMarkup([
                boundFor == whoAsking || isAdminAsking ?  unboundButton : [],
                [
                    new InlineKeyboardButton("Отмена") {CallbackData = $"keyboard:deleteMsg", Style = KeyboardButtonStyle.Primary }
                ]
            ]);
            
            await bot.SendMessage(
                whoAsking,
                "Эта группа уже привязана!",
                messageThreadId: threadId,
                replyMarkup: deBoundKeyBoard,
                cancellationToken: cancellationToken
            );
            
            return;
        }



        var isRegistered = await Memory.User.HasAuth((long) whoAsking);
        var isGroup = !ChatTools.IsPrivateChat(message);
        var isAdmin = await ChatTools.IsUserAdmin(bot, message.Chat.Id, message.From?.Id);

        if (!isGroup)
        {
            await bot.SendMessage(
                whoAsking,
            
                EscapeMarkdownV2("Эта команда предназначена для привязки групп к аутентификационным данным человека. Она недоступна в личном чате"),
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            return;
        }

        if (!isRegistered)
        {
            var sentMessage = await bot.SendMessage(
                whoAsking,
            
                EscapeMarkdownV2("Для выполнении команды сначала авторизируйтесь. "),
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            
            await Task.Delay(1300, cancellationToken);
            try
            {
                await bot.DeleteMessage(sentMessage.Chat.Id, sentMessage.MessageId, cancellationToken);
            }
            catch (Exception)
            {
                // ignored (can be deleted by humans)
            }

            return;
        }

        if (isGroup && !isAdmin)
        {
            var sentMessage = await bot.SendMessage(
                whoAsking,
            
                EscapeMarkdownV2("Для выполнении команды требуются права администратора в группе."),
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            
            await Task.Delay(1300, cancellationToken);
            try
            {
                await bot.DeleteMessage(sentMessage.Chat.Id, sentMessage.MessageId, cancellationToken);
            }
            catch (Exception)
            {
                // ignored (can be deleted by human race)
            }

            return;
        }






        const string txt = """
                           <b> Привязка к группе </b>
                           
                           Авторизируйтесь в боте и используйте кнопку ниже для привязке к группе. Вы должны быть администратором группы чтобы продолжить
                           """;

        var token = ChatTools.GenerateRandomString(5);
        await Memory.Group.setGroupBindToken(message.Chat.Id, token);

        var keyboard = new InlineKeyboardMarkup([
            [
                new InlineKeyboardButton("Привязать")
                    { CallbackData = $"bindgroup:run:{token}", Style = KeyboardButtonStyle.Primary }
            ],
            [
                new InlineKeyboardButton("Отмена") 
                    { CallbackData = "bindgroup:delete", Style = KeyboardButtonStyle.Danger }
            ]
        ]);
        
        await ChatTools.AddGroupIfNotExists(message);


        await bot.SendMessage(
            message.Chat.Id,
            text: txt,
            parseMode: ParseMode.Html,
            messageThreadId: ChatTools.GetForumId(message),
            replyMarkup: keyboard,
            receiverUserId: whoAsking,
            cancellationToken: cancellationToken);
        

        
    }
}