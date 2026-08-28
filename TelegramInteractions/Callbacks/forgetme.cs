using JetBrains.Annotations;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.TelegramInteractions.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Callbacks;


[UsedImplicitly]
[Callback("forgetme", IgnoreSplitter=false)]
public class Forgetme : ICallbackCommand {
    public async Task ExecuteAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, string[] args,
        CancellationToken cancellationToken) {
        if (callbackQuery.Message == null) return;
        
        var fromMessage = callbackQuery.From;
        var confirmation = args[0].Equals("confirm", StringComparison.OrdinalIgnoreCase);
        var erasing = args[0].Equals("erase", StringComparison.OrdinalIgnoreCase);

        if (confirmation) {
            await bot.AnswerCallbackQuery(callbackQuery.Id, "Подтвердите удаление",
                cancellationToken: cancellationToken);

            await bot.EditMessageText(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                null,
                richMessage: new InputRichMessage {
                    Html =  """
                                   <h3> Удалить всё? </h3>
                                   <h5> Вы потеряете все данные! </h5>
                                   <h5>ㅤ</h5>
                                   <br>
                                   <br>
                                   
                                   <tg-button type="callback_data" data="forgetme:erase" style="danger">Стереть</tg-button>
                                   <tg-button-row>
                                     <tg-button type="callback_data" data="keyboard:deleteMsg" style="primary">Отмена</tg-button>
                                   </tg-button-row>
                                   """
                },
                cancellationToken: cancellationToken);
            return;
        }

        if (erasing) {
            await bot.EditMessageText(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                null,
                richMessage: new InputRichMessage {
                    Html =  """
                            <h3> Секунду... </h3>
                            <p> Отвязываем все группы и стираем аутентификационные данные... </p>
                            """
                },
                cancellationToken: cancellationToken);
            
            var boundedGroups = await Memory.User.GetLinkedGroups(callbackQuery.From.Id);
            foreach (var group in boundedGroups) {
                await Memory.User.UnlinkGroup(callbackQuery.From.Id, group);
                await Memory.Group.SetGroupBind(group, null);
            }
            await Memory.User.DeleteUserAsync(callbackQuery.From.Id);
            
            await bot.EditMessageText(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                null,
                richMessage: new InputRichMessage {
                    Html =  $"""
                            <h3> Всё готово! </h3>
                            <p> Ваши аутентификационные данные были стёрты. {(boundedGroups.Count > 0 ? "Привязанные группы были отвязаны." : "")} </p>
                            
                            <tg-button-row>
                                <tg-button type="callback_data" data="keyboard:deleteMsg">Супер</tg-button>
                            </tg-button-row>
                            """
                },
                cancellationToken: cancellationToken);

            await bot.AnswerCallbackQuery(callbackQuery.Id, "Вы были стерты из базы данных",
                cancellationToken: cancellationToken);
            
            

        }

    }
}