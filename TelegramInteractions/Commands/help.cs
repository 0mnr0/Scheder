using JetBrains.Annotations;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.TelegramInteractions.Helpers;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/help")]
public class Help : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken) {


        var text = "<b>Доступные команды</b>:\n\n";
        List<BotCommand> emptyDescList = [];
        foreach (var command in EphemeralCommand.CommandsList) {
            if (string.IsNullOrEmpty(command.Description)) {
                emptyDescList.Add(command);
                continue;
            }
            
            text +=
                $"/{command.Command}{(string.IsNullOrEmpty(command.Description) ? "" : " — "+command.Description)}\n";
        }

        if (emptyDescList.Count > 0) {
            text += "\n<i>Список команд без описания</i>:\n";
            text = emptyDescList.Aggregate(text, (current, emptyCommand) => current + $"/{emptyCommand.Command}\n");
        }

        await bot.SendMessage(
            message.Chat.Id,
            text,
            parseMode: ParseMode.Html,
            messageThreadId: ChatTools.GetForumId(message),
            cancellationToken: cancellationToken);
    }
}