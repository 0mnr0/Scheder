namespace Scheder.Services;

using Telegram.Bot;
using Telegram.Bot.Types;

public interface ICallbackCommand
{
    Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken);
}