using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;

namespace Scheder.Commands;

using Telegram.Bot;
using Telegram.Bot.Types;
using Attributes;

[Command("/help")]
public class help : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        await bot.SendMessage(
            message.Chat.Id,
            """
            Доступные команды:

            /start
            /help
            """,
            messageThreadId: ChatTools.GetForumId(message),
            cancellationToken: cancellationToken);
    }
}