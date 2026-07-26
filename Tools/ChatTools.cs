using System.Text;

namespace Scheder.Tools;

using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class ChatTools
{
    public const ParseMode Markdown = ParseMode.MarkdownV2;
    

    public static int? IsForum(Message message)
    {
        // В C# свойства Chat.IsForum и MessageThreadId являются Nullable (могут быть null)
        if (message.Chat.IsForum && message.MessageThreadId != null)
        {
            return message.MessageThreadId;
        }
        return null;
    }

    public static long? WhoAsked(Message message)
    {
        return message.From?.Id;
    }

    public static string GenerateRandomString(int length)
    {
        if (length <= 0)
            return string.Empty;

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            result.Append(chars[new Random().Next(chars.Length)]);
        }

        return result.ToString();
    }
    
    public static long GetChatId(Message message)
    {
        return message.Chat.Id;
    }

    public static int? GetForumId(Message message)
    {
        return message.MessageThreadId;
    }

    public static async Task<bool> IsUserAdmin(ITelegramBotClient bot, long chatId, long? userId)
    {
        if (userId == null) return false;
        try
        {
            var member = await bot.GetChatMember(chatId, (long)userId);
            
            return member.Status is ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public static bool IsPrivateChat(Message message)
    {
        return message.Chat.Type == ChatType.Private;
    }
    
    public static bool IsGroup(Message message)
    {
        return message.Chat.Type != ChatType.Private;
    }

    public static async void AddGroupIfNotExists(Message message)
    {
        if (IsPrivateChat(message)) return;
        
        if (!await Memory.Group.IsGroupExists(message.Chat.Id))
        {
            await Memory.Group.RegisterAsync(message.Chat.Id);
        }
    }
}