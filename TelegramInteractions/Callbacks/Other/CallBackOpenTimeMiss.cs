using JetBrains.Annotations;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Callbacks.Other;


[UsedImplicitly]
[Callback("callback", IgnoreSplitter=false)]
public class CallBackOpenTimeMiss : ICallbackCommand {
    private readonly Commands.Timemiss _timeMiss = new();
    
    public async Task ExecuteAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, string[] args, CancellationToken cancellationToken) 
    {
        if (callbackQuery.Message is null) return;
        
        
        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        await _timeMiss.ExecuteAsync(bot, callbackQuery.Message, ["msgChange", args[1], callbackQuery.From.Id.ToString()], cancellationToken);
    }
}