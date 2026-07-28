using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Scheder.Tools.MDTools;

namespace Scheder.TelegramInteractions.Callbacks;


[Callback("gmt", IgnoreSplitter=false)]
public class GMT : ICallbackCommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        var isPlusOperation = args[1] == "+";
        var isMinusOperation = args[1] == "-";
        var isSaveOperation = args[1] == "save";
        var isSyncOperation = args[1] == "sync";
        var isCancelOperation = args[1] == "cancel";
        var currentUserHour = Convert.ToInt32(args[0]);

        if (isPlusOperation)
        {
            if (currentUserHour >= 23)
            {
                currentUserHour = -1;
            }

            currentUserHour += 1;
        }
        
        if (isMinusOperation)
        {
            if (currentUserHour <= 1)
            {
                currentUserHour = 24;
            }
            currentUserHour -= 1;
        }

        var currentMinutes = DateTime.Now.ToString("mm");
        var currentHour = DateTime.Now.Hour;
        
        if (isSyncOperation) { currentUserHour = currentHour; }
        var difference = currentUserHour - currentHour;
        
        if (isSaveOperation)
        {
            
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Изменения сохранены",
                cancellationToken: cancellationToken
            );
            await Memory.User.SetGMT(callbackQuery.From.Id, difference);
            await DeleteMessage(bot, callbackQuery, cancellationToken);
            return;
        }

        if (isCancelOperation)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Операция отменена",
                cancellationToken: cancellationToken
            );
            await DeleteMessage(bot, callbackQuery, cancellationToken);
            
            return;
        }
        
        
        var keyboard = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("-1 Час", $"gmt:{currentUserHour}:-"),
                InlineKeyboardButton.WithCallbackData("Синхр", $"gmt:{currentUserHour}:sync"),
                InlineKeyboardButton.WithCallbackData("+1 Час", $"gmt:{currentUserHour}:+")
            ],
            [
                new InlineKeyboardButton("Сохранить") {CallbackData = $"gmt:{currentUserHour}:save", Style = KeyboardButtonStyle.Success},
                new InlineKeyboardButton("Отмена") {CallbackData = $"gmt:{currentUserHour}:cancel", Style = KeyboardButtonStyle.Danger}
            ]
        ]);


        try
        {
            await bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                EscapeMarkdownV2($"""
                                  <b> Какое у вас время? </b>
                                  <i>Разница с сервером: {(difference > 0 ? $"+{difference}" : difference.ToString())}</i>

                                  {(currentUserHour < 10 ? $"0{currentUserHour}" : $"{currentUserHour}")}:{currentMinutes}
                                  """),
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken
            );
        }

    }
    
    
    private async Task DeleteMessage(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Message != null)
            await bot.DeleteMessage(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
    }
    
    private async Task OutOfBound(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken cancellationToken, bool IsAboveLimit)
    {

        var limitType = IsAboveLimit ? "верхнюю " : "нижнюю";
        await bot.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Вы пытаетесь выйти за {limitType} границу",
            cancellationToken: cancellationToken
        );
    }
}