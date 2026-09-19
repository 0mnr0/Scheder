using System.Security.Cryptography;
using System.Text;
using Scheder.Tools.Config;

namespace Scheder.Tools;

public class AskingHistory {
    private const int CleaningTime = 15;
    private static readonly List<ResponseObject> History = [];


    public class ResponseObject {
        public long ChatId;
        public int MsgId;
        public int AskedMessageId;
        public string Date = string.Empty;
        public string StrHash = string.Empty;
        public DateTime CreationTime = DateTime.Now;
    }

    public static bool AddResponse(long chatId, int msgId, int askedMessageId, string content, string date) {
        CleanOld();
        if (!Behaviour.Groups.AllowAskingHistory) return true; 
        
        var contentHash = GetHash(content);
        var atTheList = IsInList(chatId, msgId, contentHash, date);
        if (atTheList) {
            return false;
        }

        var obj = new ResponseObject {
            ChatId = chatId,
            MsgId = msgId,
            AskedMessageId = askedMessageId,
            Date = date,
            StrHash = contentHash,
            CreationTime = DateTime.Now
        };
        
        History.Add(obj);
        return true;
    }

    public static ResponseObject? HaveResponse(long chatId, string date) {
        CleanOld();
        
        var obj = History
            .Where(x => x.ChatId == chatId && x.Date == date)
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        return obj;
    }

    public static void DeleteResponse(ResponseObject obj) {
        History.Remove(obj);
    }
    
    
    public static string GetHash(string input) {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(bytes);

        return Convert.ToBase64String(hashBytes); 
    }
    
    private static void CleanOld() {
        var threshold = DateTime.Now.AddMinutes(-CleaningTime);

        var oldObjects = History
            .Where(x => x.CreationTime <= threshold)
            .ToList();

        foreach (var obj in oldObjects) {
            History.Remove(obj);
        }
    }

    private static bool IsInList(long chatId, int msgId, string contentHash, string date) {
        return History.Any(x =>
            x.ChatId == chatId &&
            x.MsgId == msgId &&
            x.Date == date &&
            x.StrHash == contentHash
        );
    }
}