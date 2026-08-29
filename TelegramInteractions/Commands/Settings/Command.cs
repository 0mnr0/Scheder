using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools;

namespace Scheder.TelegramInteractions.Commands.Settings;

using Services.InterfacesAndHandlers;
using Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


[Command("/settings")]
public class Command : ICommand
{

    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var fromId = message.From!.Id;
        var isGroup = ChatTools.IsGroup(message);
        var values = await SettingsService.GetEffectiveValuesAsync(chatId, isGroup, cancellationToken);
        var (text, keyboard) = SettingsUi.BuildListView(page: 0, values);

        if (isGroup && !await ChatTools.IsUserAdmin(bot, chatId, fromId)) {
            var msg = await bot.SendRichMessage(
                chatId,
                richMessage: new InputRichMessage {
                    Html = "<h5> Эта команда доступна только администраторам </h5>\n" +
                           "<tg-button type=\"callback_data\" data=\"keyboard:deleteEphMsg\">Понятно</tg-button>"
                },
                ephemeralMessageParameters: new EphemeralMessageParameters {ReceiverUserId = fromId},
                cancellationToken: cancellationToken);

            await Task.Delay(5000, cancellationToken);
            try {
                await bot.DeleteEphemeralMessage(chatId, fromId, (int)msg.EphemeralMessageId!, cancellationToken);
            }
            catch (Exception) {
                // ignored
            }
            return;
        }

        await bot.SendRichMessage(
            chatId,
            richMessage: new InputRichMessage {Html = text},
            replyMarkup: keyboard,
            ephemeralMessageParameters: isGroup ? new EphemeralMessageParameters {ReceiverUserId = fromId} : null,
            cancellationToken: cancellationToken);
    }
}