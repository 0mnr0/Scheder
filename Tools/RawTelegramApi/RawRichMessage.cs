using System.Text.Json;
using Scheder.Tools.Config;

namespace Scheder.Tools.RawTelegramApi;

public static class RawRichMessage
{
    private static readonly HttpClient Http = new();

    public static async Task Edit(long chatId, long messageId, string sentContent, byte[] imageBytes1, byte[] imageBytes2)
    {
        using var content = new MultipartFormDataContent();
 
        content.Add(new StringContent(chatId.ToString()), "chat_id");
        content.Add(new StringContent(messageId.ToString()), "message_id");
 
        const string mediaId1 = "slide1";
        const string attachFieldName1 = "photo1";
 
        const string mediaId2 = "slide2";
        const string attachFieldName2 = "photo2";
 
        var richContent = sentContent + """
                                     <tg-slideshow>
                                         <img src="tg://photo?id=slide1">
                                         <img src="tg://photo?id=slide2">
                                     </tg-slideshow>
                                     """;
 
        var inputRichMessage = new
        {
            html = richContent,
            media = new object[]
            {
                new
                {
                    id = mediaId1,
                    media = new { type = "photo", media = $"attach://{attachFieldName1}" }
                },
                new
                {
                    id = mediaId2,
                    media = new { type = "photo", media = $"attach://{attachFieldName2}" }
                }
            }
        };
 
        var richJson = JsonSerializer.Serialize(inputRichMessage);
        content.Add(new StringContent(richJson), "rich_message");
 
        var photoContent1 = new ByteArrayContent(imageBytes1);
        photoContent1.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(photoContent1, attachFieldName1, "slide1.jpg");
 
        var photoContent2 = new ByteArrayContent(imageBytes2);
        photoContent2.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(photoContent2, attachFieldName2, "slide2.jpg");
 
        var response = await Http.PostAsync(
            $"https://api.telegram.org/bot{Env.TelegramToken}/editMessageText",
            content);
    }
    
    
    public static async Task DeleteEphemeral(long chat_id, int? ephemeralMessageId, long? receiverUserId = null)
    {
        using var http = new HttpClient();
        using var content = new MultipartFormDataContent();
 
        content.Add(new StringContent(chat_id.ToString()), "chat_id");
        content.Add(new StringContent(ephemeralMessageId.ToString()), "ephemeral_message_id");
 
        if (receiverUserId is not null)
            content.Add(new StringContent(receiverUserId.Value.ToString()), "receiver_user_id");
 
        var response = await http.PostAsync(
            $"https://api.telegram.org/bot{Env.TelegramToken}/deleteEphemeralMessage",
            content);
    }
}