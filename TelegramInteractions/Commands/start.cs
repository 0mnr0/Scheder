using JetBrains.Annotations;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Scheder.TelegramInteractions.Commands;


[UsedImplicitly]
[Command("/start")]
public class Start : ICommand
{
    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = message.From?.Id;
        if (whoAsking == null) return;
        var isGroup = ChatTools.IsGroup(message);
        var threadId = ChatTools.GetForumId(message);

        await ChatTools.AddGroupIfNotExists(message);


        var wasRegistered = await Memory.User.IsUserExistsAsync((long) whoAsking);
        // var isGroup = ChatTools.IsForum(message);

        var txt = $"""
                   <h3> Доброе время суток! </h3>
                   
                   </h5> Это бот для получения расписания для Top-Academy. Чтобы начать работать с ботом {(isGroup ? 
                           "привяжите группу с помощью команды /bindgroup."
                           
                           : "пожалуйста пришлите ваш логин и пароль в формате:\n<p>ㅤ</p>" + 
                             "<blockquote> /auth MyLogin, MyPassword </blockquote>" +
                            (wasRegistered ? "Этой командой вы так-же можете изменить ваш логин и пароль если вы уже зарегистрированы" : "")
                           )}
                   </h5>
                   """;
        
        await bot.SendRichMessage(
            message.Chat.Id,
            richMessage: new InputRichMessage {
                Html = txt
            },
            messageThreadId: threadId,
            cancellationToken: cancellationToken);
    }
}