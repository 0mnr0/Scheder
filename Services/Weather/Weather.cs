using Scheder.Services.Database;
using Scheder.Tools;
using Telegram.Bot.Types;

namespace Scheder.Services.Weather;

public class Weather
{

    private static async Task<string?> GetUserCity(long userId) {
        return await Memory.User.GetCity(userId);
    }

    private static async Task<List<WeatherAPI.WeatherObject>?> GetDirectWeather(string userCity, string day, bool isGroup = false)
    {
        if (Config.Env.WeatherApiToken == null) return null;
        if (!userCity.Contains('/')) return await WeatherAPI.Get(userCity, day);
        
        var data = userCity.Split('/');
        userCity = data[^1];
        return await WeatherAPI.Get(userCity, day);
    }


    public static async Task<List<WeatherAPI.WeatherObject>?> GetWeather(long uid, BestDayOption.BestDayParseResult dayObject, bool isGroup = false)
    {
        if (dayObject.IsWeek) return null;
        if (Config.Env.WeatherApiToken == null) return null;
        var day = dayObject.StartDate;

        if (isGroup) {
            var newUid = await Memory.Group.getGroupBind(uid);
            if (!newUid.HasValue) return null;
            uid = newUid.Value;
        }

        var targetCity = await GetCity(uid);
        if (targetCity == null) return null;
        
        var cachedWeather =
            CachedWeatherService.DateExists(targetCity, day)
                ? CachedWeatherService.GetText(targetCity, day)
                : null;
        
        if (cachedWeather != null)
        {
            Console.WriteLine("[WeatherXCache] Sending cached weather response!");
            return cachedWeather;
        }

        var response = await GetDirectWeather(targetCity, day, isGroup);
        if (response == null) return null;
        
        CachedWeatherService.Add(targetCity, day, response);
        return response;
    }


    private static async Task<string?> GetCity(long uid, bool isGroup = false) {
        if (isGroup) {
            var newUid = await Memory.Group.getGroupBind(uid);
            if (!newUid.HasValue) return null;
            uid = newUid.Value;
        }

        var targetCity = await GetUserCity(uid);
        return targetCity;
    }

    public static async Task SetRichImageUrls(
            List<byte[]> imageData,
            long uid,
            BestDayOption.BestDayParseResult dayObject,
            bool isGroup = false
        ) {
            var city = await GetCity(uid, isGroup);
            if (city == null) return;
            var targetDate = dayObject.StartDate;
            
            CachedWeatherService.AddRichImages(city, targetDate, imageData);
    }

    public static async Task<List<InputRichMessageMedia>?> GetRichImageUrls(
        long uid,
        BestDayOption.BestDayParseResult dayObject,
        bool isGroup = false
    ) {
        
        var city = await GetCity(uid, isGroup);
        if (city == null) return null;
        var targetDate = dayObject.StartDate;

        var list = CachedWeatherService.GetRichImages(city, targetDate);
        return list;
    }

}