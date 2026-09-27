using System.Text.Json;
using Scheder.Services.Database.Helpers;
using Scheder.Tools.Config;
using Scheder.Tools.Proxy;
using static Scheder.Tools.Logger;

namespace Scheder.Services.Weather;

public class WeatherAPI
{
    private static HttpClient _client = new();


    public static void Init() {
        _client.Timeout = TimeSpan.FromSeconds(0.8);
        if (!Env.UseProxyForWeather) return;
        
        var proxy = Proxy.SetAutoProxy(true);
        if (proxy is null) return;
        
        proxy.Timeout = TimeSpan.FromSeconds(0.8);
        _client = proxy;
    }

    public static async Task<List<WeatherObject>?> Get(string city, string date) {

        var parseUrl = $"https://api.weatherapi.com/v1/forecast.json?key={Env.WeatherApiToken}&q={city}&dt={date}";
        // var json = await _client.GetStringAsync(parseUrl);

        var json = string.Empty;
        for (var i = 0; i < 4; i++) {
            try {
                var response = await _client.GetAsync(parseUrl);
                if (i > 0) {
                    Log.Warning("[WeatherAPI] Fail Counter: Iteration {S} = Code {A}", i+1, response.StatusCode);
                }
                await NewStat.OnNewStat(StatDefinition.FETCH_WEATHER_API, ((int)response.StatusCode).ToString());
                if (!response.IsSuccessStatusCode) {
                    continue;
                }

                json = await response.Content.ReadAsStringAsync();
                break;
            }
            catch (Exception e) {
                await NewStat.OnNewStat(StatDefinition.FETCH_WEATHER_API_TIMEOUT, "Timeout");
                Log.Warning("[WeatherAPI] Fail Counter: Iteration {S} = Fail: {e}", i+1, e.Message);
            }
        }
        if (string.IsNullOrEmpty(json)) return null;
        
        var doc = JsonDocument.Parse(json);
        var forecast = doc.RootElement
            .GetProperty("forecast")
            .GetProperty("forecastday");

        Log.Information("[WeatherAPI] ({S}): {A}", parseUrl, json);
        if (forecast.GetArrayLength() == 0) return null;
        
        var hours = forecast[0].GetProperty("hour");


        List<WeatherObject> weatherStat = [];
        foreach (var hour in hours.EnumerateArray())
        {
            var time = DateTime.Parse(hour.GetProperty("time").GetString()!);
            

            if (time.Hour is not (9 or 12 or 16 or 21)) continue;

            var weatherBlock = new WeatherObject(time.ToString("HH:mm"))
            {
                Temp = hour.GetProperty("temp_c").GetDouble(),
                Condition = hour.GetProperty("condition").GetProperty("code").GetInt32()

            };

            weatherBlock.WeatherTitle = WeatherAssoc.getOpinion(weatherBlock.Condition);
            weatherBlock.WeatherIcon = WeatherAssoc.getIcon(weatherBlock.Condition);
            weatherBlock.WeatherTextIcon = WeatherAssoc.getTextIcon(weatherBlock.Condition);
            
            weatherStat.Add(
                weatherBlock
            );
        }
        
        
        return weatherStat;
    }

    public class WeatherObject(string time)
    {
        public string Time { get; set; } = time;
        public double Temp { get; set; }
        public int Condition { get; set; }
        public string? WeatherTitle { get; set; }
        public string? WeatherIcon { get; set; }
        public string? WeatherTextIcon { get; set; }
    }
}