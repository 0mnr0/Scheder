using JetBrains.Annotations;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.iWillBeLate;
using Scheder.TelegramInteractions.Attributes;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.TelegramInteractions.Callbacks;


[UsedImplicitly]
[Callback("timemiss", IgnoreSplitter=false)]
public class Timemiss : ICallbackCommand {
    
    public async Task ExecuteAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, string[] args,
        CancellationToken cancellationToken) {
        if (callbackQuery.Message?.From is null) return;
        var whoAsked = callbackQuery.From.Id;
        
        var send15M = args[0] == "w15";
        var send30M = args[0] == "w30";
        var sendUnk = args[0] == "unk";
        var sendRem = args[0] == "rem";
        var isPrivateChat = ChatTools.IsPrivateChat(callbackQuery.Message);

        if (Env.TimeMissApi is null) {
            await bot.AnswerCallbackQuery(
                callbackQuery.Id,
                "Функция отключена в настройках окружения",
                showAlert: true,
                cancellationToken: cancellationToken
            );
            return;
        }

        if (!await Memory.User.IsUserExistsAsync(whoAsked)) {
            await bot.AnswerCallbackQuery(
                callbackQuery.Id,
                "Для этой функции требуется авторизация в боте. Выполните её в личных сообщениях.",
                showAlert: true,
                cancellationToken: cancellationToken
            );
            return;
        }

        var pushResult = string.Empty;
        if (send15M) {pushResult = "15";}
        if (send30M) {pushResult = "30";}
        if (sendUnk) {pushResult = "?";}



        DirectTimeMissApi.MissedTimeSendResult actionResult = null!;
        if (send15M || send30M || sendUnk) {
            actionResult = await TimeMissApi.Update(
                whoAsked,
                pushResult
            );
        }

        if (sendRem) {
            actionResult = await TimeMissApi.Delete(
                whoAsked
            );
        }

        if (send15M || send30M || sendUnk || sendRem) {
            await bot.AnswerCallbackQuery(
                callbackQuery.Id,
                actionResult.ShowText,
                cancellationToken: cancellationToken
            );

            if (isPrivateChat) {
                await bot.EditMessageText(
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    null,
                    richMessage: new InputRichMessage {
                        Html = """
                               <h4> Готово! </h4>
                               <p> С нашей стороны всё чисто. Мы предали информацию, теперь дело за расширением и внимательностью преподавателя. </p>
                               
                               </hr>
                               
                               <tg-button-row>
                                 <tg-button type="callback_data" data="callback:openTimemiss:0">Назад</tg-button>
                                 <tg-button type="callback_data" data="keyboard:deleteMsg" style="primary">Закрыть</tg-button>
                               </tg-button-row>
                               """
                    },
                    cancellationToken: cancellationToken
                );
            }
            else {
                
                await bot.EditEphemeralMessageText(
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.From.Id,
                    (int)callbackQuery.Message.EphemeralMessageId!,
                    null,
                    richMessage: new InputRichMessage {
                        Html = """
                               <h4> Готово! </h4>
                               <p> С нашей стороны всё чисто. Мы предали информацию, теперь дело за расширением и внимательностью преподавателя. </p>

                               </hr>

                               <tg-button-row>
                                 <tg-button type="callback_data" data="callback:openTimemiss:1">Назад</tg-button>
                                 <tg-button type="callback_data" data="keyboard:deleteMsg" style="primary">Закрыть</tg-button>
                               </tg-button-row>
                               """
                    },
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}