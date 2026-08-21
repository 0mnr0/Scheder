using Telegram.Bot;
using Telegram.Bot.Types;
using static Scheder.Tools.Logger;

namespace Scheder.TelegramInteractions.Helpers;

public static class EphemeralCommand {
    public static readonly BotCommand[] CommandsList = [
        new() {
            Command = "auth",
            Description = "Привязать данные к аккаунту или обновить их"
        },
        new() {
            Command = "bindgroup",
            Description = "Привязать группу к моему аккаунту",
            IsEphemeral = true
        },
        new() {
            Command = "datewatch",
            Description = "Управление прослушиваемыми датами"
        },
        new() {
            Command = "gmt",
            Description = "Управление и синхронизация времени"
        },
        new() {
            Command = "help",
            Description = string.Empty
        },
        new() {
            Command = "unbound",
            Description = "Отвязать группу"
        },
        new() {
            Command = "settings",
            Description = "Настройка бота для текущего чата"
        },
        new() {
            Command = "sched",
            Description = "Показать пары (аргументы поддерживаются)"
        },
        new() {
            Command = "start",
            Description = string.Empty
        }
    ];



    public static async Task AddAll(TelegramBotClient bot)
    {

        Log.Information("[Ephemeral] Patching commands...");
        await bot.SetMyCommands([]); // not working without this piece of 
        
        await bot.SetMyCommands(CommandsList);
        Log.Information("[Ephemeral] Done!");
    }
}