using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        }
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);

        return Task.CompletedTask;
    }
}