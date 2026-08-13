using Scheder.Tools;

namespace Scheder.TelegramInteractions.Commands.Settings;

using Data;
using Services.InterfacesAndHandlers;
using Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


[Callback("setting", IgnoreSplitter=false)]
public class Callback : ICallbackCommand
{

    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is null || args.Length == 0)
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var userId = callbackQuery.From.Id;
        var ephData = callbackQuery.Message.EphemeralMessageId;
        var isGroup = ChatTools.IsGroup(callbackQuery.Message);
        var editAsEphemeral = ChatTools.IsGroup(callbackQuery.Message) && ephData != null;
        
        switch (args[0])
        {
            case "noop":
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;

            case "l":
            {
                var page = int.Parse(args[1]);
                var values = await SettingsService.GetEffectiveValuesAsync(chatId, isGroup, cancellationToken);
                var (text, keyboard) = SettingsUi.BuildListView(page, values);

                if (editAsEphemeral && ephData != null) {

                    await bot.EditEphemeralMessageText(
                        chatId, userId, ephData.Value,
                        text: text, replyMarkup: keyboard, cancellationToken: cancellationToken
                    );

                }
                else {
                    
                    await bot.EditMessageText(chatId, messageId, text,
                        parseMode: ParseMode.None, replyMarkup: keyboard, cancellationToken: cancellationToken);
                }
                
                
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            case "o":
            {
                var id = int.Parse(args[1]);
                var page = int.Parse(args[2]);
                var def = SettingsRegistry.GetById(id);
                if (def is null)
                {
                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Настройка не найдена", cancellationToken: cancellationToken);
                    return;
                }

                var value = await SettingsService.GetEffectiveValueAsync(chatId, def, isGroup, cancellationToken);
                var (text, keyboard) = SettingsUi.BuildDescriptionView(def, value, page);

                if (editAsEphemeral && ephData != null) {

                    await bot.EditEphemeralMessageText(
                        chatId, userId, ephData.Value,
                        text: text, replyMarkup: keyboard, cancellationToken: cancellationToken
                        );

                }
                else {
                    await bot.EditMessageText(chatId, messageId, text,
                        parseMode: ParseMode.None, replyMarkup: keyboard, cancellationToken: cancellationToken);
                }


                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            case "t":
            {
                var id = int.Parse(args[1]);
                var ctx = args[2];
                var page = int.Parse(args[3]);
                var def = SettingsRegistry.GetById(id);
                if (def is null)
                {
                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Настройка не найдена", cancellationToken: cancellationToken);
                    return;
                }

                var newValue = await SettingsService.ToggleAsync(chatId, def, isGroup, cancellationToken);

                if (ctx == "d")
                {
                    var (text, keyboard) = SettingsUi.BuildDescriptionView(def, newValue, page);
                    
                    if (editAsEphemeral && ephData != null) {

                        await bot.EditEphemeralMessageText(
                            chatId, userId, ephData.Value,
                            text: text, replyMarkup: keyboard, cancellationToken: cancellationToken
                        );

                    }
                    else {
                        await bot.EditMessageText(chatId, messageId, text,
                            parseMode: ParseMode.None, replyMarkup: keyboard, cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    var values = await SettingsService.GetEffectiveValuesAsync(chatId, isGroup, cancellationToken);
                    var (text, keyboard) = SettingsUi.BuildListView(page, values);
                    await bot.EditMessageText(chatId, messageId, text,
                        parseMode: ParseMode.None, replyMarkup: keyboard, cancellationToken: cancellationToken);
                }

                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            default:
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
        }
    }
}