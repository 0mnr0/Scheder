using Scheder.TelegramInteractions.Commands.Settings.Data;

namespace Scheder.TelegramInteractions.Commands.Settings;

using Services.InterfacesAndHandlers;
using Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


[Command("/settings")]
public class Callback : ICommand
{

    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var values = await SettingsService.GetEffectiveValuesAsync(message.From!.Id, cancellationToken);
        var (text, keyboard) = SettingsUi.BuildListView(page: 0, values);
        
        await bot.SendMessage(
            message.Chat.Id,
            text,
            parseMode: ParseMode.None,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}