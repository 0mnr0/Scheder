using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Scheder.Tools;
using static Scheder.Tools.Logger;

namespace Scheder.Services.JournalAPI;

public class API
{
    private static readonly HttpClient Client;

    static API()
    {
        Client = new HttpClient();
        
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        Client.DefaultRequestHeaders.Add("origin", "https://journal.top-academy.ru");
        Client.DefaultRequestHeaders.Add("referer", "https://journal.top-academy.ru/");
    }
    
    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? authToken)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(authToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
        return request;
    }

    public static async Task<HttpResponseMessage> GetAsync(string url, string? authToken)
    {
        using var request = CreateRequest(HttpMethod.Get, url, authToken);
        return await Client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostAsync(string url, object jsonPayload, string? authToken = null)
    {
        using var request = CreateRequest(HttpMethod.Post, url, authToken);
        request.Content = JsonContent.Create(jsonPayload);
        return await Client.SendAsync(request);
    }

    public static async Task<(JsonNode?, string[])> GetAuthAsync(string login, string pass)
    {
        var payload = new
        {
            application_key = "6a56a5df2667e65aab73ce76d1dd737f7d1faef9c52e8b8c55ac75f565d8e8a6",
            password = pass,
            username = login
        };

        List<string> tries = [];
        for (var i=1; i < 4; i++) // 3 cycles (starts from 1)
        {
            var delay = 230 + (i * 40);
            try
            {
                Log.Information("[Journal API] Making request ({I}/3) ({D}ms)", i, delay);
                var response = await PostAsync("https://msapi.top-academy.ru/api/v2/auth/login", payload);
                Log.Information("[Journal API]: Response code: {ResponseStatusCode}", response.StatusCode);
                tries.Add(""+(int)response.StatusCode);
                
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await Task.Delay(delay);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();
                return (JsonNode.Parse(content), [.. tries]);
            }
            catch (Exception ex)
            {
                Log.Warning("[Journal API]: Failed request: {ms1}, {ms2}", ex.Message, ex.Source);
                tries.Add("500?");
                await Task.Delay(delay);
            }
        }

        return (null, [.. tries]);
    }

    public static async Task<(string?, string[])> GetTokenAsync(string login, string password)
    {
        var (authResult, tries) = await GetAuthAsync(login, password);
        return (authResult?["access_token"]?.ToString(), tries);
    }

    public static async Task<string?> GetCityAsync(string login, string password)
    {
        var (authResult, _) = await GetAuthAsync(login, password);
        return authResult?["city_data"]?["timezone_name"]?.ToString();
    }

    public static Task<string?> GetTokenAsync(JsonNode? payload)
    {
        return Task.FromResult(payload?["access_token"]?.ToString());
    }

    public static Task<string?> GetCityAsync(JsonNode? payload)
    {
        return Task.FromResult(payload?["city_data"]?["timezone_name"]?.ToString());
    }


    
    
    
    public static async Task<SchedResponse> GetSched(string token, string date, string? endDate, PerformanceMetric? metric)
    {
        using (metric?.Measure(MetricType.DataParse)) {
            endDate ??= date;

            var response = await GetAsync(
                @$"https://msapi.top-academy.ru/api/v2/schedule/operations/get-by-date-range?date_start={date}&date_end={endDate}",
                token
            );

            var returnValue = new SchedResponse {
                Code = (int)response.StatusCode,
                Message = await response.Content.ReadAsStringAsync()
            };

            return returnValue;
        }
    }
    
    
    public static async Task<ExamsResponse> GetExams(string token, PerformanceMetric? metric = null)
    {
        using (metric?.Measure(MetricType.DataParse)) {
            var response = await GetAsync(
                @$"https://msapi.top-academy.ru/api/v2/progress/operations/student-exams",
                token
            );

            var returnValue = new ExamsResponse {
                Code = (int)response.StatusCode,
                Message = await response.Content.ReadAsStringAsync()
            };

            return returnValue;
        }
    }


    
    
    
    public class SchedResponse
    {
        public int Code { get; set; }
        public string? Message { get; set; }
    }
    
    public class ExamsResponse
    {
        public int Code { get; set; }
        public string? Message { get; set; }
    }

}