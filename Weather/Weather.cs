using DotNetEnv;
using Scheder.JournalAPI;
using Scheder.Tools;

namespace Scheder;

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

        var targetCity = await GetUserCity(uid);
        Console.WriteLine("targetCity: "+targetCity);
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
}