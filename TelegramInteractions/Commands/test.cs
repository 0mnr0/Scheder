using JetBrains.Annotations;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/test")]
public class Test : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {

        for (var i = 0; i < 30; i++) {
            await Task.Delay(100 * i, cancellationToken);
            _ = API.GetTokenAsync("", "");
        }

        /*var chatId = message.Chat.Id;
        var threadId = message.MessageThreadId;
        Console.WriteLine($"Current thread: {threadId}, msgId: {message.MessageId}");


        await bot.SendMessage(
            chatId: chatId,
            text: "Hey",
            messageThreadId: threadId, // if its "General", its "null"
            replyParameters: new ReplyParameters {

                // target message to reply, is in threadId = 1303
                MessageId = 1361, // target msg id
            },
            cancellationToken: cancellationToken);*/
    }
}