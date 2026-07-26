namespace Scheder.Services;

using Telegram.Bot;
using Telegram.Bot.Types;

public interface ITextHandler
{
    Task HandleAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken);
}