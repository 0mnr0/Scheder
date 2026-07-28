using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.Services.InterfacesAndHandlers;

public interface ITextHandler
{
    Task HandleAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken);
}