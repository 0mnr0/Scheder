using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.Services.InterfacesAndHandlers;

public interface ICommand
{
    Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken);
}
