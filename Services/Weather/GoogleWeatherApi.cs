using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Scheder.Services.Database.Helpers;
using Scheder.Tools.Config;
using Scheder.Tools.Proxy;
using static Scheder.Tools.Logger;

namespace Scheder.Services.Weather;

public abstract class GoogleWeatherApi
{
    private const string ForecastUrl = "https://weather.googleapis.com/v1/forecast/hours:lookup";
    private const string HistoryUrl = "https://weather.googleapis.com/v1/history/hours:lookup";
    private const string GeocodeUrl = "https://maps.googleapis.com/maps/api/geocode/json";

    private const int MaxRetries = 4;
    private const int PageSize = 24;
    private const int MaxPages = 10;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly ConcurrentDictionary<string, (double Lat, double Lng)> GeoCache = new();

    private static HttpClient _client = new();

    public static void Init() {
        _client.Timeout = TimeSpan.FromSeconds(4);
        if (!Env.UseProxyForWeather) return;

        var proxy = Proxy.SetAutoProxy(true);

        proxy.Timeout = TimeSpan.FromSeconds(4);
        _client = proxy;
    }

    private static readonly int[] Slots = [9, 12, 16, 21];

    public static async Task<List<WeatherObject>?> Get(string city, string date) {
        if (!DateOnly.TryParse(date, Inv, out var targetDate)) {
            Log.Warning("[GoogleWeather] Bad date format: {D}", date);
            return null;
        }

        var coords = await Geocode(city);
        if (coords is null) return null;
        var (lat, lng) = coords.Value;

        var loc = $"&location.latitude={lat.ToString(Inv)}&location.longitude={lng.ToString(Inv)}&unitsSystem=METRIC";
        var forecastUrl = $"{ForecastUrl}?key={Env.GoogleApiToken}{loc}&hours=240&pageSize={PageSize}";
        var historyUrl = $"{HistoryUrl}?key={Env.GoogleApiToken}{loc}&hours=24&pageSize={PageSize}";

        Dictionary<int, WeatherObject> slots = new();
        DateOnly? firstForecastDate = null;
        string? pageToken = null;
        var pages = 0;
        var reachedEnd = false;

        
        while (pages < MaxPages && !reachedEnd) {
            var url = pageToken is null
                ? forecastUrl
                : $"{forecastUrl}&pageToken={Uri.EscapeDataString(pageToken)}";

            var json = await FetchJson(url, trackStats: true);
            if (string.IsNullOrEmpty(json)) break;
            pages++;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("forecastHours", out var hours)) break;

            var (end, first) = CollectSlots(hours, targetDate, slots);
            reachedEnd = end;
            firstForecastDate ??= first;

            pageToken = root.TryGetProperty("nextPageToken", out var np) ? np.GetString() : null;
            if (string.IsNullOrEmpty(pageToken)) break;
        }

        
        if (slots.Count < Slots.Length && firstForecastDate is { } fd && targetDate <= fd) {
            var json = await FetchJson(historyUrl, trackStats: true);
            if (!string.IsNullOrEmpty(json)) {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("historyHours", out var hours)) {
                    CollectSlots(hours, targetDate, slots);
                }
                pages++;
            }
        }
        
        Log.Information("[GoogleWeather] ({City}, {Date}): requests: {P}, slots: {N}", city, date, pages, slots.Count);

        return slots.Count == 0 ? null : [.. slots.OrderBy(kv => kv.Key).Select(kv => kv.Value)];
    }


    private static (bool ReachedLater, DateOnly? FirstDate) CollectSlots(
        JsonElement hours, DateOnly targetDate, Dictionary<int, WeatherObject> slots) {
        var reachedLater = false;
        DateOnly? firstDate = null;

        foreach (var hour in hours.EnumerateArray()) {
            var local = hour.GetProperty("displayDateTime");

            var hourDate = new DateOnly(
                local.GetProperty("year").GetInt32(),
                local.GetProperty("month").GetInt32(),
                local.GetProperty("day").GetInt32());
            var h = local.TryGetProperty("hours", out var hp) ? hp.GetInt32() : 0;

            firstDate ??= hourDate;

            if (hourDate > targetDate) { reachedLater = true; continue; }
            if (hourDate < targetDate) continue;
            if (Array.IndexOf(Slots, h) < 0 || slots.ContainsKey(h)) continue;

            var temp = 0.0;
            if (hour.TryGetProperty("temperature", out var t) && t.TryGetProperty("degrees", out var d))
                temp = d.GetDouble();

            var type = hour.TryGetProperty("weatherCondition", out var wc) && wc.TryGetProperty("type", out var ty)
                ? ty.GetString()
                : null;

            var weatherBlock = new WeatherObject($"{h:00}:00")
            {
                Temp = temp,
                ConditionType = type,
                WeatherTitle = WeatherAssoc.GoogleApi.GetOpinion(type),
                WeatherIcon = WeatherAssoc.GoogleApi.GetIcon(type),
                WeatherTextIcon = WeatherAssoc.GoogleApi.GetTextIcon(type)
            };

            slots[h] = weatherBlock;
        }

        return (reachedLater, firstDate);
    }

    private static async Task<(double Lat, double Lng)?> Geocode(string city) {
        var cacheKey = city.Trim().ToLowerInvariant();
        if (GeoCache.TryGetValue(cacheKey, out var cached)) return cached;

        var json = await FetchJson(
            $"{GeocodeUrl}?address={Uri.EscapeDataString(city)}&key={Env.GoogleApiToken}",
            trackStats: false);
        if (string.IsNullOrEmpty(json)) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = root.GetProperty("status").GetString();
        if (status != "OK") {
            Log.Warning("[GoogleWeather] Geocode failed for {City}: {S}", city, status);
            return null;
        }

        var loc = root.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
        var coords = (loc.GetProperty("lat").GetDouble(), loc.GetProperty("lng").GetDouble());

        GeoCache[cacheKey] = coords;
        return coords;
    }

    private static async Task<string?> FetchJson(string url, bool trackStats) {
        for (var i = 0; i < MaxRetries; i++) {
            try {
                using var response = await _client.GetAsync(url);
                if (i > 0) {
                    Log.Warning("[GoogleWeather] Fail Counter: Iteration {S} = Code {A}", i + 1, response.StatusCode);
                }

                if (trackStats)
                    await NewStat.OnNewStat(StatDefinition.FETCH_WEATHER_API, ((int)response.StatusCode).ToString());

                if (!response.IsSuccessStatusCode) continue;

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e) {
                if (trackStats)
                    await NewStat.OnNewStat(StatDefinition.FETCH_WEATHER_API_TIMEOUT, "Timeout");
                Log.Warning("[GoogleWeather] Fail Counter: Iteration {S} = Fail: {e}", i + 1, e.Message);
            }
        }

        return null;
    }
    
    private static int ToLegacyCode(string? type) => type switch {
        "CLEAR" or "MOSTLY_CLEAR" => 1000,
        "PARTLY_CLOUDY" => 1003,
        "MOSTLY_CLOUDY" or "WINDY" => 1006,
        "CLOUDY" => 1009,

        "CHANCE_OF_SHOWERS" => 1063,
        "LIGHT_RAIN" => 1183,
        "LIGHT_TO_MODERATE_RAIN" => 1186,
        "RAIN" or "WIND_AND_RAIN" => 1189,
        "MODERATE_TO_HEAVY_RAIN" or "RAIN_PERIODICALLY_HEAVY" => 1192,
        "HEAVY_RAIN" => 1195,
        "LIGHT_RAIN_SHOWERS" or "SCATTERED_SHOWERS" => 1240,
        "RAIN_SHOWERS" => 1243,
        "HEAVY_RAIN_SHOWERS" => 1246,

        "CHANCE_OF_SNOW_SHOWERS" => 1066,
        "LIGHT_SNOW" or "LIGHT_TO_MODERATE_SNOW" => 1213,
        "SNOW" => 1219,
        "HEAVY_SNOW" or "MODERATE_TO_HEAVY_SNOW" or "SNOW_PERIODICALLY_HEAVY" => 1225,
        "LIGHT_SNOW_SHOWERS" or "SCATTERED_SNOW_SHOWERS" => 1255,
        "SNOW_SHOWERS" or "HEAVY_SNOW_SHOWERS" => 1258,
        "BLOWING_SNOW" => 1114,
        "RAIN_AND_SNOW" => 1207,

        "HAIL_SHOWERS" => 1261,
        "HAIL" => 1264,

        "THUNDERSTORM" or "SCATTERED_THUNDERSTORMS" => 1087,
        "LIGHT_THUNDERSTORM_RAIN" => 1273,
        "THUNDERSHOWER" or "HEAVY_THUNDERSTORM" => 1276,
        "SNOWSTORM" or "HEAVY_SNOW_STORM" => 1282,

        _ => 1003 // TYPE_UNSPECIFIED / новые типы, которые Google добавит позже
    };
}