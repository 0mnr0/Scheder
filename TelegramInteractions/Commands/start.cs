using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot.Types.Enums;
using static Scheder.Tools.MDTools;

namespace Scheder.Commands;

using Telegram.Bot;
using Telegram.Bot.Types;
using Attributes;

[Command("/start")]
public class start : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = message.From?.Id;
        if (whoAsking == null)
        {
            return;
        }

        ChatTools.AddGroupIfNotExists(message);


        var wasRegistered = await Memory.User.IsUserExistsAsync((long) whoAsking);
        var isGroup = ChatTools.IsForum(message);

        var txt = $"""
                   Доброе время суток! Это бот для получения расписания для Top-Academy. Чтобы начать работать с ботом, пожалуйста пришлите ваш логин и пароль в формате:

                   <blockquote> /auth MyLogin, MyPassword </blockquote>
                   {(wasRegistered ? "Этой командой вы так-же можете изменить ваш логин и пароль если вы уже зарегистрированы" : "")}
                   """;
        
        await bot.SendMessage(
            whoAsking,
            txt,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}