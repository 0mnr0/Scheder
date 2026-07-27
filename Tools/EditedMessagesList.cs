using Telegram.Bot.Types;
namespace Scheder.Tools;

public class EditedMessagesList {
    private class MessageData {
        public long ChatId {get; init;}
        public long MessageId {get; init;}
    }
    private static readonly Lock Lock = new();
    private static readonly List<MessageData> MessagesList = [];


    public static async void AddMessage(long chatId, long messageId, int? secondsToClear = 90) {
        lock (Lock) {
            MessagesList.Add(new MessageData {
                ChatId = chatId,
                MessageId = messageId
            });
        }
        
        if (secondsToClear == null) return;
        var seconds = (int)secondsToClear;
        await Task.Delay(seconds * 1000);
    }

    public static void AddMessage(Message message) {
        AddMessage(message.Chat.Id, message.MessageId);
    }

    public static bool IsIn(long chatId, long messageId) {
        lock (Lock) {
            return MessagesList.Any(x =>
                x.ChatId == chatId &&
                x.MessageId == messageId);
        }
    }

    public static void Delete(long chatId, long messageId) {
        lock (Lock) {
            MessagesList.RemoveAll(x =>
                x.ChatId == chatId &&
                x.MessageId == messageId);
        }
    }
}