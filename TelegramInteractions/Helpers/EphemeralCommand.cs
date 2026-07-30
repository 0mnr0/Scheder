using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Helpers;

public static class EphemeralCommand
{
    public static async Task AddAll(TelegramBotClient bot)
    {

        Console.WriteLine("[Ephemeral] Patching commands...");
        await bot.SetMyCommands([]); // not working without this piece of 
        
        await bot.SetMyCommands([
            new BotCommand
            {
                Command = "auth",
                Description = "Привязать данные к аккаунту или обновить их",
                IsEphemeral =  true
            },
            new BotCommand
            {
                Command = "bindgroup",
                Description = "Привязать группу к моему аккаунту",
                IsEphemeral =  true
            },
            new BotCommand
            {
                Command = "unbound",
                Description = "Отвязать группу",
                IsEphemeral =  true
            },
            new BotCommand
            {
                Command = "settings",
                Description = "Настройка бота для текущего чата",
                IsEphemeral =  true
            },
        ]);
        Console.WriteLine("[Ephemeral] Done ");
    }
}