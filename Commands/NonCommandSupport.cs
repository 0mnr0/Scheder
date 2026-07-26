using Scheder.ContextDetection;

namespace Scheder.Commands;
using Services;
using Telegram.Bot;
using Telegram.Bot.Types;

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
                await _schedCommand.ExecuteAsync(bot, message, [], cancellationToken);
                return;
        }
    }
    
}
