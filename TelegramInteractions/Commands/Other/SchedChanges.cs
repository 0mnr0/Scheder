using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands.Other;

public class SchedChanges {
    
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        long chatId,
        int? threadId,
        string messageText,
        CancellationToken cancellationToken
    ) {
        
        
        await bot.SendRichMessage(
            chatId,
            new InputRichMessage { Html = messageText },
            messageThreadId: threadId,
            cancellationToken: cancellationToken
            );
        
    }
}