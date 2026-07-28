using Scheder.Commands;
using Scheder.Services.ContextDetection;
using Scheder.Services.InterfacesAndHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands;

public class NonCommandSupport : ITextHandler
{
    private readonly Sched _schedCommand = new();
    public async Task HandleAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        var text = message.Text;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text))
            return;

        if (!text.StartsWith("пар", StringComparison.CurrentCultureIgnoreCase))
        {
            return;
        }

        var ratio = DetectionContextRatio.GetRatio(text);
        switch (ratio)
        {
            case < DetectionContextRatio.DefaultThreshold:
                return;
            case >= DetectionContextRatio.DefaultThreshold:
                await _schedCommand.ExecuteAsync(bot, message, ["directMessage", ratio*100+""], cancellationToken);
                return;
        }
    }
    
}
