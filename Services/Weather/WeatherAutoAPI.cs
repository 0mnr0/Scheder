using Scheder.Tools.Config;

namespace Scheder.Services.Weather;

public abstract class WeatherAutoApi {
    public static async Task<List<WeatherObject>?> Get(string city, string date) {
        if (!Env.AppendWeather) {
            return null;
        }
        
        if (Env.UseWeatherApi) {
            if (string.IsNullOrEmpty(Env.WeatherApiToken)) {
                return null;
            }
            
            return await WeatherApi.Get(city, date);
        }

        if (string.IsNullOrEmpty(Env.GoogleApiToken)) return null;
        return await GoogleWeatherApi.Get(city, date);
    }

    public static void Init() {
        if (!Env.AppendWeather) {
            return;
        }
        
        if (Env.UseWeatherApi) {
            WeatherApi.Init();
        }
        else {
            GoogleWeatherApi.Init();
        }
    }
}