using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Scheder.Tools.Logger;

namespace Scheder.Services.InterfacesAndHandlers;

public class UpdateHandler : IUpdateHandler
{
    private readonly CommandHandler _commandHandler;
    private readonly CallbackInterface _callbackInterface;

    public UpdateHandler(CommandHandler commandHandler, CallbackInterface callbackInterface)
    {
        _commandHandler = commandHandler;
        _callbackInterface = callbackInterface;
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _ = ProcessUpdateAsync(botClient, update, cancellationToken);
    }
    
    private async Task ProcessUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message when update.Message != null:
                    await _commandHandler.HandleAsync(
                        botClient,
                        update.Message,
                        cancellationToken);
                    break;

                case UpdateType.CallbackQuery:
                    await _callbackInterface.HandleCallbackAsync(
                        botClient,
                        update,
                        cancellationToken);
                    break;
                
                case UpdateType.Unknown:
                case UpdateType.InlineQuery:
                case UpdateType.ChosenInlineResult:
                case UpdateType.EditedMessage:
                case UpdateType.ChannelPost:
                case UpdateType.EditedChannelPost:
                case UpdateType.ShippingQuery:
                case UpdateType.PreCheckoutQuery:
                case UpdateType.Poll:
                case UpdateType.PollAnswer:
                case UpdateType.MyChatMember:
                case UpdateType.ChatMember:
                case UpdateType.ChatJoinRequest:
                case UpdateType.MessageReaction:
                case UpdateType.MessageReactionCount:
                case UpdateType.ChatBoost:
                case UpdateType.RemovedChatBoost:
                case UpdateType.BusinessConnection:
                case UpdateType.BusinessMessage:
                case UpdateType.EditedBusinessMessage:
                case UpdateType.DeletedBusinessMessages:
                case UpdateType.PurchasedPaidMedia:
                case UpdateType.ManagedBot:
                case UpdateType.GuestMessage:
                case UpdateType.Subscription:
                    break;
                default:
                    Log.Error("[Telegram.Bot] Unknown update type: {UpdateType}", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Telegram.Bot] Error: {er}", ex);
        }
    }

    

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        Log.Fatal("[Telegram.Bot]: {e}", exception);

        return Task.CompletedTask;
    }
}