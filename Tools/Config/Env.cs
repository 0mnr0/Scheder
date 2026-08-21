using Scheder.Services.Database;

namespace Scheder.Tools.Config;

public class Env
{
    public static readonly string? TelegramToken = Environment.GetEnvironmentVariable("TG_TOKEN");
    public static readonly string? WeatherApiToken = Environment.GetEnvironmentVariable("Weather_Token") ?? null;
    public static readonly string? DbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    public static readonly string? DbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    public static readonly string? DbName = Environment.GetEnvironmentVariable("DB_NAME") ?? DatabaseTools.DatabaseName;
    public static readonly string? DbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    public static readonly string? DbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? "postgres";
    public static readonly string? PreFetchData = Environment.GetEnvironmentVariable("PreFetchData") ?? "{}";
    public static readonly bool DisableEarlyDayFix = GetBool("DisableEarlyDayFix", false);
    public static readonly bool FastStart = GetBool("FastStart");
    public static readonly long DebugUid = GetLong("DebugUID", 0);
    
    // PROXY
    public static readonly bool UseProxy = GetBool("UseProxy");
    public static readonly string? ProxyLine = Environment.GetEnvironmentVariable("ProxyConnection");
    
    // CACHE
    private static readonly bool DisableCaching = GetBool("DisableCaching");
    private static readonly bool DisableTokenCaching = GetBool("DisableTokenCaching");

    // OTHER
    public static readonly bool TalkativePerformance = GetBool("FastStart");
    public static readonly bool ProdPrepare = GetBool("ProdPrepare");
    
    // NON-DOCUMENTED-ENV-VARs (For testing purposes only)
    public static readonly int EarlyDayFixTrigger = GetInt("EarlyDayFixTrigger", 1);
    

    public static void UpdateRules() {
        if (DisableCaching) {
            Behaviour.Other.AllowScheduleCaching = false;
            Behaviour.Other.AllowWeatherCaching = false;
        }
        
        if (DisableTokenCaching) {
            Behaviour.Other.AllowTokenCaching = false;
        }
    }
    
    private static bool GetBool(string name) {
        return string.Equals(Environment.GetEnvironmentVariable(name), "true", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool GetBool(string name, bool defValue) {
        return string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)) ?  defValue : GetBool(name);
    }
    
    
    private static long GetLong(string name, long defValue) {
        var envVal = Environment.GetEnvironmentVariable(name);
        if (envVal is not null && long.TryParse(envVal, out var result)) {
            return result;
        }

        return defValue;
    }
    
    
    private static int GetInt(string name, int defValue) {
        var envVal = Environment.GetEnvironmentVariable(name);
        if (envVal is not null && int.TryParse(envVal, out var result)) {
            return result;
        }

        return defValue;
    }
}