using Scheder.Services.ContextDetection;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands.Other;

public class NonCommandSupport : ITextHandler
{
    private readonly Sched _schedCommand = new();
    public async Task HandleAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken) {

        var state = await SettingsService.GetValue(message.Chat.Id, SettingsList.ContextDetection, ChatTools.IsGroup(message), cancellationToken);
        if (state is null or 0) return; // not found or context detection is disabled
        var fullDetection = state.Value == 2;
        
        
        var text = message.Text;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text))
            return;

        
        if (!fullDetection && !text.StartsWith("пар", StringComparison.CurrentCultureIgnoreCase))
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
