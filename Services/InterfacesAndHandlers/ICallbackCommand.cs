using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.Services.InterfacesAndHandlers;

public interface ICallbackCommand
{
    Task ExecuteAsync(
        ITelegramBotClient bot,
        CallbackQuery callbackQuery,
        string[] args,
        CancellationToken cancellationToken);
}