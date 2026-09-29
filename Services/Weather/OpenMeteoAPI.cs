using Scheder.Tools.Config;
using Scheder.Tools.Proxy;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Scheder.Services.Database.Helpers;
using static Scheder.Tools.Logger;

namespace Scheder.Services.Weather;

public class OpenMeteoAPI
{
    private static HttpClient _client = new();

    private static readonly ConcurrentDictionary<string, (double Lat, double Lon)> GeoCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly int[] TargetHours = [9, 12, 16, 21];

    public static void Init() {
        _client.Timeout = TimeSpan.FromSeconds(2);
        if (!Env.UseProxyForWeather) return;

        var proxy = Proxy.SetAutoProxy(true);

        proxy.Timeout = TimeSpan.FromSeconds(2);
        _client = proxy;
    }

    public static async Task<List<WeatherObject>?> Get(string city, string date) {
        var coords = await ResolveCity(city);
        if (coords is null) return null;

        var url = string.Create(CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={coords.Value.Lat}&longitude={coords.Value.Lon}" +
            $"&hourly=temperature_2m,weather_code&start_date={date}&end_date={date}&timezone=auto");

        var json = await FetchJson(url);
        if (string.IsNullOrEmpty(json)) return null;

        Log.Information("[OpenMeteoAPI] ({S}): Size: {A}", url, json.Length);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("hourly", out var hourly)) return null;

        var times = hourly.GetProperty("time");
        var temps = hourly.GetProperty("temperature_2m");
        var codes = hourly.GetProperty("weather_code");

        var count = times.GetArrayLength();
        if (count == 0) return null;

        List<WeatherObject> weatherStat = [];
        for (var i = 0; i < count; i++) {
            // время приходит в локальной тайм-зоне города (timezone=auto), формат "2026-09-29T09:00"
            var time = DateTime.Parse(times[i].GetString()!, CultureInfo.InvariantCulture);
            if (!TargetHours.Contains(time.Hour)) continue;

            if (temps[i].ValueKind != JsonValueKind.Number || codes[i].ValueKind != JsonValueKind.Number)
                continue;

            var weatherBlock = new WeatherObject(time.ToString("HH:mm"))
            {
                Temp = temps[i].GetDouble(),
                Condition = WmoToWeatherApi(codes[i].GetInt32())
            };

            weatherBlock.WeatherTitle = WeatherAssoc.WeatherApi.GetOpinion(weatherBlock.Condition);
            weatherBlock.WeatherIcon = WeatherAssoc.WeatherApi.GetIcon(weatherBlock.Condition);
            weatherBlock.WeatherTextIcon = WeatherAssoc.WeatherApi.GetTextIcon(weatherBlock.Condition);

            weatherStat.Add(weatherBlock);
        }

        return weatherStat;
    }

    private static async Task<(double Lat, double Lon)?> ResolveCity(string city) {
        if (GeoCache.TryGetValue(city, out var cached)) return cached;

        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&format=json";
        var json = await FetchJson(url);
        if (string.IsNullOrEmpty(json)) return null;

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) {
            Log.Warning("[OpenMeteoAPI] Geocoding: city not found: {C}", city);
            return null;
        }

        var first = results[0];
        var coords = (first.GetProperty("latitude").GetDouble(), first.GetProperty("longitude").GetDouble());
        GeoCache[city] = coords;
        return coords;
    }

    private static async Task<string?> FetchJson(string url) {
        for (var i = 0; i < 4; i++) {
            try {
                var response = await _client.GetAsync(url);
                if (i > 0) {
                    Log.Warning("[OpenMeteoAPI] Fail Counter: Iteration {S} = Code {A}", i + 1, response.StatusCode);
                }
                await NewStat.OnNewStat(StatDefinition.FETCH_WEATHER_API, ((int)response.StatusCode).ToString());
                if (!response.IsSuccessStatusCode) continue;

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e) {
                await NewStat.OnNewStat(StatDefinition.FETCH_WEATHER_API_TIMEOUT, "Timeout");
                Log.Warning("[OpenMeteoAPI] Fail Counter: Iteration {S} = Fail: {e}", i + 1, e.Message);
            }
        }
        return null;
    }


    private static int WmoToWeatherApi(int wmo) => wmo switch
    {
        0 => 1000,  // ясно
        1 => 1000,  // преимущественно ясно
        2 => 1003,  // переменная облачность
        3 => 1009,  // пасмурно
        45 => 1135, // туман
        48 => 1147, // изморозевый туман
        51 => 1150, // лёгкая морось
        53 => 1153, // морось
        55 => 1153, // сильная морось
        56 => 1168, // ледяная морось
        57 => 1171, // сильная ледяная морось
        61 => 1183, // небольшой дождь
        63 => 1189, // умеренный дождь
        65 => 1195, // сильный дождь
        66 => 1198, // ледяной дождь
        67 => 1201, // сильный ледяной дождь
        71 => 1213, // небольшой снег
        73 => 1219, // умеренный снег
        75 => 1225, // сильный снег
        77 => 1237, // снежные зёрна
        80 => 1240, // небольшой ливень
        81 => 1243, // ливень
        82 => 1246, // сильный ливень
        85 => 1255, // небольшой снегопад (ливневый)
        86 => 1258, // сильный снегопад (ливневый)
        95 => 1276, // гроза
        96 => 1276, // гроза с градом
        99 => 1276, // гроза с сильным градом
        _ => 1006   // неизвестный код -> облачно
    };
}