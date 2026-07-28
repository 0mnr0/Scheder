namespace Scheder.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

public interface ICommand
{
    Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken);
}
