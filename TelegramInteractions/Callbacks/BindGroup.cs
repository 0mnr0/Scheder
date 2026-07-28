using Scheder.Attributes;
using Scheder.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Scheder.Tools.MDTools;

namespace Scheder.Callbacks;

[Callback("bindgroup", IgnoreSplitter=false)]
public class BindGroup : ICallbackCommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var message = callbackQuery.Message;
        var chatId =  message!.Chat.Id;
        var msgId = (int) message.EphemeralMessageId!;
        var isCancel = args[0]=="delete";
        var token = isCancel ? null : args[1];
        
        
        if (!await Memory.User.HasAuth(userId))
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Для начала зарегистрируйтесь в боте",
                cancellationToken: cancellationToken
            );
            return;
        }


        if (await Memory.Group.getGroupBindToken(chatId) != token || isCancel)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: isCancel ? "Отменено" : "Ключ безопасности более не актуален",
                cancellationToken: cancellationToken
            );
            await bot.DeleteEphemeralMessage(
                chatId,
                receiverUserId: userId,
                msgId,
                cancellationToken);
            return;
        }

        await bot.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Подтвердите привязку в Личных сообщениях",
            cancellationToken: cancellationToken
        );
        
        




        var keyboard = new InlineKeyboardMarkup(
            new InlineKeyboardButton("Я понял, привязать") {CallbackData = $"allowbind:{chatId}:{token}:yes:{msgId}", Style = KeyboardButtonStyle.Danger},
            new InlineKeyboardButton("Отмена") {CallbackData = $"allowbind:{chatId}:{token}:cancel", Style = KeyboardButtonStyle.Primary}
        );
        
        
        var confirmation =  await bot.SendMessage(userId,
            EscapeMarkdownV2("Вы точно хотите привязать группу к своему аккаунту? Все вызовы API будут проходить через ваши данные для аутентификации. "),
            replyMarkup: keyboard,
            parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
        

    }
}
