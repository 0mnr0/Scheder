using JetBrains.Annotations;
using Scheder.Services.ContextDetection;
using Scheder.Services.Database;
using Scheder.Services.DateWatcher;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.TelegramInteractions.Commands;

[UsedImplicitly]
[Command("/datewatch")]
public class Datewatch : ICommand {
    
    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, string[] args, CancellationToken cancellationToken) {
        if (message.Text is null) return;
        
        var showAsList = args.Length == 0;
        var chatId = message.Chat.Id;
        var whoAsked = message.From!.Id;
        var isGroup = ChatTools.IsGroup(message);
        var threadId = ChatTools.GetForumId(message);
        var isUserNotAllowed = isGroup && !Behaviour.Groups.AllowNonAdminsToDateWatch && !await ChatTools.IsUserAdmin(bot, chatId, whoAsked);
        // asking user is in group && this command is not allowed to run for usual users && he is not admin
        
        if (isGroup && isUserNotAllowed) {
            var msg = await bot.SendMessage(
                chatId: chatId,
                "Эту команду можно выполнять только администраторам группы!",
                messageThreadId: threadId,
                receiverUserId: whoAsked,
                cancellationToken: cancellationToken
            );
            
            await Task.Delay(5000, cancellationToken);
            await bot.DeleteEphemeralMessage(message.Chat.Id, whoAsked, (int)msg.EphemeralMessageId!, cancellationToken: cancellationToken);
            
            return;
        }

        
        IEntitySource memorySource = isGroup ? new GroupSource() : new UserSource();
        if (showAsList) {
            var watchers = await memorySource.GetDayListeners(chatId);
            
            
            var nav = watchers
                .Select(watcher => new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"Уведомления на: {watcher.Date}",
                        $"datewatch:cm:{watcher.Date}"
                    )
                })
                .ToList();

            nav.Add([
                new InlineKeyboardButton("Закрыть")
                    { CallbackData = $"keyboard:deleteMsg", Style = KeyboardButtonStyle.Primary }

            ]);

            var keyboard = new InlineKeyboardMarkup(nav);
            
            
            var text = "С помощью команды <b>/datewatch</b> можно установить наблюдатель на будущую дату (в формате dd-mm-yyyy). Просто укажите дату или условный день после команды. Например:\n\n" +
                       "<blockquote> /datewatch 02.10</blockquote> Или <blockquote> /datewatch завтра</blockquote>" +
                       "\n\nКак только расписание на указанный день появится или изменится - бот об этом напишет в течении ~получаса." +
                       (watchers.Count > 0 ? "\nЧтобы отключить прослушиватель просто нажмите на него из этого списка:" : "\nАктивные прослушиватели можно будет отключить этой же командой");

            await bot.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        var currentListeners = await memorySource.GetDayListeners(chatId);

        
        if (currentListeners.Count >= Behaviour.MaxDayListenersPerChat) {
            await bot.SendMessage(
                chatId: chatId,
                "Вы достигли максимального количества прослушивателей доступных для этого чата",
                messageThreadId: threadId,
                cancellationToken: cancellationToken);
            return;
        }
        
        var day = DateExtractor.GetDay(message.Text, null);
        var parsedDay = await BestDayOption.Get(chatId, day, fromGroup: isGroup, ignoreEarlyDay: true);
        
        if (currentListeners.Any(listener => listener.Date == parsedDay.StartDate)) {
            await bot.SendMessage(
                chatId: chatId,
                "Прослушиватель на эту дату уже установлен!",
                messageThreadId: threadId,
                cancellationToken: cancellationToken);
            return;
        }
        
        await memorySource.AddDayListener(chatId, parsedDay.StartDate, threadId);
        await bot.SendMessage(
            chatId,
            $"<b> Оповестим об изменениях на {Declensions.GetDeclensionDayTitle(parsedDay.DateStart)}! </b>\n" +
                    $"(Формат Journal: {parsedDay.StartDate}), ",
            messageThreadId: threadId,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

    }
}