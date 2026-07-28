using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands;


[Command("/exams")]
public class exams : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoTriggered = message.From!.Id;
        var chatId = message.Chat.Id;
        var isGroup = ChatTools.IsGroup(message);
        
        
        
    }
}