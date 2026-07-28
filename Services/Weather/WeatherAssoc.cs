namespace Scheder.Services.Weather;

public class WeatherAssoc
{
    public static string getOpinion(int code)
    {
        return code switch
        {
            1000 => "Ясно",
            1003 => "Малооблачно",
            1006 => "Облачно",
            1009 => "Пасмурно",
            1030 => "Туман",
            1012 or 1015 or 1018 or 1021 or 1024 or 1027 or 1033 or 1036 => "Дым",
            1039 or 1042 => "Смог",
            1045 or 1048 => "Пыль",
            1063 => "Возможен дождь",
            1066 or 1069 => "Возможен снег",
            1072 => "Возможна морось",
            1087 => "Возможна гроза",
            1114 or 1117 => "Метель",
            1135 or 1147 => "Туман",
            1150 or 1153 or 1168 or 1171 => "Возможна морось",
            1180 or 1183 or 1198 => "Легкий дождь",
            1186 or 1189 or 1240 or 1243 or 1246 => "Дождь",
            1192 or 1195 or 1201 => "Сильный дождь",
            1204 or 1207 => "Мокрый снег",
            1210 or 1213 => "Слабый снег",
            1216 or 1219 => "Снег",
            1222 or 1225 => "Сильный снег",
            1237 or 1261 or 1264 => "Град",
            1249 or 1252 => "Дождь со снегом",
            1273 or 1276 => "Дождь с грозой",
            1279 or 1282 => "Снег с грозой",
            _ => $"ХЗ ({code})"
        };
    }
    
    
    public static string getIcon(int code)
    {
        return code switch
        {
            1000 => "clear_day",
            1003 => "clear_with_cloudy",
            1006 => "cloudy_with_clear",
            1009 => "cloudy",
            1012 or 1015 or 1018 or 1021 or 1024 or 1027 => "haze_fog",
            1030 => "haze_fog",
            1033 or 1036 => "haze_fog",
            1039 or 1042 => "haze_fog",
            1045 or 1048 => "haze_fog",
            1063 => "cloudy_then_rain",
            1066 or 1069 => "cloudy_then_snow",
            1072 => "sleet_hail",
            1087 => "strong_thunderstorms",
            1114 or 1117 => "blowing_snow",
            1135 or 1147 => "haze_fog",
            1150 or 1153 or 1168 or 1171 => "sleet_hail",
            1180 or 1183 or 1198 => "drizzle",
            1186 or 1189 or 1240 or 1243 or 1246 => "cloudy_with_rain",
            1192 or 1195 or 1201 => "rain_showers",
            1204 or 1207 => "snow_with_rain",
            1210 or 1213 => "cloudy_with_snow",
            1216 or 1219 => "cloudy_with_snow",
            1222 or 1225 => "heavy_snow",
            1237 or 1261 or 1264 => "icy",
            1249 or 1252 => "wintry_mix",
            1273 or 1276 => "thunderstorms",
            1279 or 1282 => "thunderstorms",
            _ => $"not_available"
        };
    }
    
    public static string getTextIcon(int code)
    {
        return code switch
        {
            1000 => "☀",
            1003 => "⛅",
            1006 => "⛅",
            1009 => "☁",
            1030 => "🌫",
            1012 or 1015 or 1018 or 1021 or 1024 or 1027 or 1033 or 1036 => "🌫",
            1039 or 1042 => "🌫",
            1045 or 1048 => "🌫",
            1063 => "🌧",
            1066 or 1069 => "🌨",
            1072 => "☔",
            1087 => "☔",
            1114 or 1117 => "🌫",
            1135 or 1147 => "🌫",
            1150 or 1153 or 1168 or 1171 => "☔",
            1180 or 1183 or 1198 => "🌧",
            1186 or 1189 or 1240 or 1243 or 1246 => "🌧",
            1192 or 1195 or 1201 => "🌧",
            1204 or 1207 => "🌨",
            1210 or 1213 => "🌨",
            1216 or 1219 => "🌨",
            1222 or 1225 => "🌨",
            1237 or 1261 or 1264 => "☔",
            1249 or 1252 => "☔",
            1273 or 1276 => "☔",
            1279 or 1282 => "☔",
            _ => $"❓({code})"
        };
    }
}