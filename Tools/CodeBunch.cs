using Scheder.Services.Database;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheder.Tools;

public class CodeBunch
{
    public static async Task<long?> GetUidFromGroup(long groupId)
    {
        var result = await Memory.Group.GetGroupBind(groupId);
        return result;
    }
    
    
    public static async Task<bool> RegValidCheck(Message message) {
        var command = message.Text!.Split(" ")[0];
        if (Behaviour.NonRegisteredUserCanInteractOnlyWithThisCommands.Contains(command)) {
            return true; // skip if user in the group
        }
        
        var fromUser = message.From!.Id;
        return await Memory.User.IsUserExistsAsync(fromUser);
    }


    public static long GetUnixFromDateTime(BestDayOption.BestDayParseResult day, List<SchedMessageBuilder.Lesson> lessonList)
    {

        var (date, targetTime) = GetFirstLesson(day, lessonList);
        targetTime ??= "09:00";
        
        var time = TimeSpan.Parse(targetTime);

        var result = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            time.Hours,
            time.Minutes,
            0,
            date.Kind 
        );
        
        return new DateTimeOffset(result).ToUnixTimeSeconds();
    }

    private static (DateTime, string?) GetFirstLesson(BestDayOption.BestDayParseResult day, List<SchedMessageBuilder.Lesson> lessonList)
    {
        string? result = null;
        if (lessonList.Count > 0)
        {
            result = (lessonList[0]).StartedAt;
        }
        return (day.DateStart, result);
    }


    public static async Task<InlineKeyboardMarkup> GetGroupList(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken, bool isEphemeral = false)
    {
        var linkedGroups = await Memory.User.GetLinkedGroups(chatId);
        var keyboard = new InlineKeyboardMarkup();
        foreach (var groupId in linkedGroups)
        {
                
            var chatName = (await bot.GetChat(groupId, cancellationToken: cancellationToken)).Title ?? $"ID: {groupId}";

            keyboard.AddNewRow(
                new InlineKeyboardButton(chatName + $"|unbound:{groupId}")
                    { CallbackData = $"unbound:{groupId}", Style = KeyboardButtonStyle.Danger }
            );
        }
        
        keyboard.AddNewRow(
            new InlineKeyboardButton("Отмена")
                { CallbackData = $"keyboard:{(isEphemeral ? "deleteEphMsg" : "deleteMsg")}", Style = KeyboardButtonStyle.Primary }
        );

        return keyboard;
    }
}