using Scheder.Services.InterfacesAndHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Commands.Other;

public class Nonreg : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsked = message.Chat.Id;
        await bot.SendMessage(whoAsked, "Вы не зарегистрированы. Чтобы использовать бота нужны аутентификационные данные. Используйте /auth", cancellationToken: cancellationToken);

    }
}