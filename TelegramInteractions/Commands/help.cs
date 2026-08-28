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


        var text = "<h3>Доступные команды:</h3><br> </br>";
        List<BotCommand> emptyDescList = [];
        foreach (var command in EphemeralCommand.CommandsList) {
            if (string.IsNullOrEmpty(command.Description) || command.Description.Equals(EphemeralCommand.EmptySymbol)) {
                emptyDescList.Add(command);
                continue;
            }
            
            text +=
                $"/{command.Command}{(string.IsNullOrEmpty(command.Description) ? "" : " — "+command.Description)}<br> </br>";
        }

        if (emptyDescList.Count > 0) {
            text += "<br> </br><br> </br><i>Список команд без описания</i>:<br> </br>";
            text = emptyDescList.Aggregate(text, (current, emptyCommand) => current + $"/{emptyCommand.Command}<br> </br>");
        }

        text += "<br> </br> <tg-button type=\"url\" style=\"primary\" url=\"https://github.com/0mnr0/Scheder\">GitHub</tg-button>";

        await bot.SendRichMessage(
            message.Chat.Id,
            richMessage: new InputRichMessage {
                Html = text
            },
            messageThreadId: ChatTools.GetForumId(message),
            cancellationToken: cancellationToken);
    }
}