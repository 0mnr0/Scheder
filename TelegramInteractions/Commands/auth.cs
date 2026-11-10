using JetBrains.Annotations;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Scheder.Tools.MDTools;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/auth")]
public class Auth : ICommand
{
    private enum AuthStep
    {
        Login,
        Password
    }

    private sealed class PendingAuth
    {
        public AuthStep Step;
        public string Login = string.Empty;
        public DateTime StartedAt = DateTime.Now;
    }

    private static readonly Dictionary<long, PendingAuth> Pending = new();
    private static readonly Lock PendingLock = new();
    private static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(5);


    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var whoAsking = message.From?.Id;
        var msgTxt = message.Text;
        if (whoAsking == null || msgTxt == null)
        {
            return;
        }

        var isGroup = ChatTools.IsGroup(message);

        if (isGroup) {
            const string alertTitle =  "<h4>Ни в коем случае больше так не делайте </h4>\n";
            const string alertText = 
                "<p>Не выполняйте авторизацию в группах, так вы можете показать ваш пароль и логин всем участникам группы</p>" ;
            
            var alertMsg = await bot.SendRichMessage(
                message.Chat.Id,
                richMessage:  new InputRichMessage { Html = alertTitle + alertText + "\n<p> Мы пытаемся удалить это сообщение... </p>" },
                messageThreadId: ChatTools.GetForumId(message),
                ephemeralMessageParameters: new EphemeralMessageParameters {ReceiverUserId = (long)whoAsking},
                cancellationToken: cancellationToken
            );

            try {
                await bot.DeleteMessage(
                    chatId: message.Chat.Id,
                    messageId: message.Id,
                    cancellationToken: cancellationToken
                );
                
                await bot.EditEphemeralMessageText(
                    alertMsg.Chat.Id,
                    (long)whoAsking,
                    (int)alertMsg.EphemeralMessageId!,
                    null,
                    richMessage: new InputRichMessage {
                        Html = alertTitle + alertText +"<p> Настоятельно рекомендуется сменить пароль </p>"
                    },
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception) {
                await bot.EditEphemeralMessageText(
                    alertMsg.Chat.Id,
                    (long)whoAsking,
                    (int)alertMsg.EphemeralMessageId!,
                    null,
                    richMessage: new InputRichMessage {
                        Html = "<h1> Удалите ваше сообщение! </h1> \n <h2> У бота нет прав на удаление сообщений! </h2>"
                               + alertText
                    },
                    cancellationToken: cancellationToken
                );
            }
            
            return;
        }

        var uid = (long)whoAsking;
        CancelPending(uid); // /auth всегда начинает ввод заново

        var (login, password, tooManyArgs) = ParseCredentials(args);

        if (tooManyArgs) {
            await bot.SendRichMessage(
                message.Chat.Id,
                richMessage: new InputRichMessage {
                    Html = "<h3> Неправильное количество аргументов </h3>"
                            + "<p> Пожалуйста, используйте один из вариантов: </p> \n<br> </br>"
                            + "<blockquote> <b>/auth login, password</b> </blockquote>\n<br> </br>"
                            + "<p> или отправьте <b>/auth</b> без аргументов — бот попросит логин и пароль отдельными сообщениями </p>"
                },
                messageThreadId: ChatTools.GetForumId(message),
                cancellationToken: cancellationToken
            );
            return;
        }

        // Всё в одном сообщении, как и раньше
        if (login.Length > 0 && password.Length > 0) {
            await AuthorizeAsync(bot, message, uid, login, password, cancellationToken);
            return;
        }

        StartPending(uid, login);
        await AskNextAsync(bot, message, login.Length > 0, cancellationToken);
    }


    /// <summary>
    /// Обрабатывает сообщение, если пользователь сейчас вводит логин или пароль по шагам.
    /// Возвращает true, если сообщение поглощено диалогом авторизации.
    /// </summary>
    public static async Task<bool> TryHandlePendingAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        if (!ChatTools.IsPrivateChat(message))
            return false;

        var whoAsking = message.From?.Id;
        var text = message.Text;
        if (whoAsking == null || text == null)
            return false;

        var uid = (long)whoAsking;

        // Пользователь передумал и вызвал другую команду - отпускаем её в обычную обработку
        if (text.StartsWith('/'))
        {
            CancelPending(uid);
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string login;
        var isPasswordStep = false;

        lock (PendingLock)
        {
            CleanOld();

            if (!Pending.TryGetValue(uid, out var pending))
                return false;

            if (pending.Step == AuthStep.Login)
            {
                pending.Login = text.Trim();
                pending.Step = AuthStep.Password;
                pending.StartedAt = DateTime.Now;
                login = pending.Login;
            }
            else
            {
                login = pending.Login;
                Pending.Remove(uid);
                isPasswordStep = true;
            }
        }

        if (isPasswordStep)
            await AuthorizeAsync(bot, message, uid, login, text.Trim(), cancellationToken);
        else
            await AskNextAsync(bot, message, loginKnown: true, cancellationToken);

        return true;
    }


    private static (string login, string password, bool tooManyArgs) ParseCredentials(string[] args)
    {
        var raw = string.Join(" ", args).Trim();
        if (raw.Length == 0)
            return (string.Empty, string.Empty, false);

        // Старый формат: пробелы стираются, логин и пароль разделяются запятой
        var parts = raw.Replace(" ", "").Split(',');
        if (parts.Length > 2)
            return (string.Empty, string.Empty, true);

        var login = parts[0];
        var password = parts.Length == 2 ? parts[1] : string.Empty;

        return (login, password, false);
    }

    private static void StartPending(long uid, string login)
    {
        lock (PendingLock)
        {
            CleanOld();
            Pending[uid] = new PendingAuth
            {
                Step = login.Length > 0 ? AuthStep.Password : AuthStep.Login,
                Login = login
            };
        }
    }

    private static void CancelPending(long uid)
    {
        lock (PendingLock)
        {
            Pending.Remove(uid);
        }
    }

    private static void CleanOld()
    {
        var threshold = DateTime.Now - PendingLifetime;
        var expired = Pending
            .Where(x => x.Value.StartedAt <= threshold)
            .Select(x => x.Key)
            .ToList();

        foreach (var uid in expired)
            Pending.Remove(uid);
    }

    private static Task AskNextAsync(
        ITelegramBotClient bot,
        Message message,
        bool loginKnown,
        CancellationToken cancellationToken)
    {
        var html = loginKnown
            ? """
              <h3> Авторизация </h3>
              <p> Логин принят. Теперь пришлите <b>пароль</b> отдельным сообщением </p>
              <p> Отменить ввод можно командой /start </p>
              """
            : """
              <h3> Авторизация </h3>
              <p> Нужны аутентификационные данные, пришлите их в формате: </p>
              <blockquote> login, password </blockquote>
              <b> И обязательно с запятой! </b>
              <br><p>ㅤ</p><p>ㅤ</p>
              
              <h4> Или </h4>
              <p> Пришлите ваш <b>логин</b> одним сообщением, следующим сообщением нужно будет прислать <b>пароль</b> </p>
              <br>
              <p> Отменить ввод можно командой /start </p>
              """;

        return bot.SendRichMessage(
            message.Chat.Id,
            richMessage: new InputRichMessage { Html = html },
            messageThreadId: ChatTools.GetForumId(message),
            cancellationToken: cancellationToken);
    }


    private static async Task AuthorizeAsync(
        ITelegramBotClient bot,
        Message message,
        long whoAsking,
        string login,
        string password,
        CancellationToken cancellationToken)
    {
        var wasRegistered = await Memory.User.IsUserExistsAsync(whoAsking);

        var msg = await bot.SendMessage(
            chatId: message.Chat.Id,
            EscapeMarkdownV2("Дайте пару секунд, нужно проверить логин и пароль..."),
            parseMode: ParseMode.MarkdownV2,
            messageThreadId: ChatTools.GetForumId(message),
            cancellationToken: cancellationToken);

        var (payload, authRes) = await API.GetAuthAsync(login, password);
        if (payload == null)
        {
            await bot.EditMessageText(
                message.Chat.Id,
                msg.Id,
                null,
                richMessage: new InputRichMessage {
                    Html = $"""
                                <h4> Неудача :( </h4>
                                <p> Не удалось вас авторизовать, проверьте данные от входа и попробуйте ещё раз </p>
                                <p>ㅤ</p>
                                <details>
                                    <summary>Подробнее</summary>
                                    <p> Код ошибок: {string.Join(", ",  authRes)} </p>
                                    <hr/>
                                    <i>Иногда сервера Top Academy не отвечают успешной авторизацией даже при правильных аутентификационных данных, попробуйте позже. </i>
                                </details>
                           """
                },
                cancellationToken: cancellationToken);
        }
        else
        {
            if (!wasRegistered)
            {
                await Memory.User.RegisterAsync(whoAsking);
            }

            await Memory.User.SaveAuthAsync(
                whoAsking,
                login,
                password
            );

            await Memory.User.SetJwtAsync(
                whoAsking,
                payload["access_token"]!.ToString()
            );

            await Memory.User.SetCity(
                whoAsking,
                payload["city_data"]?["timezone_name"]?.ToString()
            );

            await bot.EditMessageText(
                message.Chat.Id,
                msg.Id,
                null,
                richMessage: new InputRichMessage {
                    Html = "<h3>Супер!</h3>\n<p>Теперь вы зарегистрированы в боте, можете пользоваться всеми доступными командами</p>"
                },
                cancellationToken: cancellationToken);
        }
    }
}
