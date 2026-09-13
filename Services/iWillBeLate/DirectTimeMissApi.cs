using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Scheder.Tools.Config;

namespace Scheder.Services.iWillBeLate;

public class DirectTimeMissApi {
    private static readonly HttpClient Client = new();
    private static readonly string? EndPoint = Env.TimeMissApi;

    
    // Uses URL: _endPoint + "/update" 
    public static async Task<MissedTimeSendResult?> Update(
            long groupId,
            long tgId,
            int cityId,
            long studentId,
            string minutesLate,
            string userName,
            string userTime,
            string targetDate
        ) {
        
        var data = new {
            groupId,
            tgId,
            cityId,
            userName,
            studentId,
            userTime,
            targetDate,
            minutesLate,
            action = "update"
        };
        
        
        var request = new HttpRequestMessage(HttpMethod.Post, EndPoint + "/update")
        {
            Content = JsonContent.Create(data) 
        };
        request.Headers.Add("TrustedProgram", "SchederAPP");
        
        var response = await Client.SendAsync(request);

        if (!response.IsSuccessStatusCode) {
            return new MissedTimeSendResult {
                Success = false,
                Message = string.Empty,
                Action = 1
            };
        }
        
        var res = await response.Content.ReadFromJsonAsync<MissedTimeSendResult>();
        res?.Action = 1;
        return res;
    }
    
    
    
    // Uses URL: _endPoint + "/update" 
    public static async Task<MissedTimeSendResult?> Delete(
            long groupId,
            long tgId,
            int cityId
        ) {
        
        var data = new {
            groupId,
            tgId,
            cityId,
            action = "delete"
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, EndPoint + "/update")
        {
            Content = JsonContent.Create(data) 
        };
        request.Headers.Add("TrustedProgram", "SchederAPP");
        
        var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode) {
            return new MissedTimeSendResult {
                Success = false,
                Message = string.Empty,
                Action = 2
            };
        }
        
        var res = await response.Content.ReadFromJsonAsync<MissedTimeSendResult>();
        res?.Action = 2;
        return res;
    }
    
    
    
    
    public class MissedTimeSendResult 
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("reason")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("innerCode")]
        public int? InnerCode { get; set; } = -100;
        
        public string? ShowText { get; set; } = string.Empty;
        
        public int? Action { get; set; } = 0;
    } 
}