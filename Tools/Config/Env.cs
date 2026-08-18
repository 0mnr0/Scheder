using Scheder.Services.Database;

namespace Scheder.Tools.Config;

public class Env
{
    public static readonly string? TelegramToken = Environment.GetEnvironmentVariable("TG_TOKEN");
    public static readonly string? WeatherApiToken = Environment.GetEnvironmentVariable("Weather_Token") ?? null;
    public static readonly long DebugUid = Environment.GetEnvironmentVariable("DebugUID") != null ? long.Parse(Environment.GetEnvironmentVariable("DebugUID")!) : 0;
    public static readonly string? DB_HOST = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    public static readonly string? DB_PORT = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    public static readonly string? DB_NAME = Environment.GetEnvironmentVariable("DB_NAME") ?? DatabaseTools.DatabaseName;
    public static readonly string? DB_USER = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    public static readonly string? DB_PASS = Environment.GetEnvironmentVariable("DB_PASS") ?? "postgres";
    public static readonly string? PreFetchData = Environment.GetEnvironmentVariable("PreFetchData") ?? "{}";
    public static readonly bool FastStart = GetBool("FastStart");
    
    // PROXY
    public static readonly bool UseProxy = GetBool("UseProxy");
    public static readonly string? ProxyLine = Environment.GetEnvironmentVariable("ProxyConnection");
    
    // CACHE
    private static readonly bool DisableCaching = GetBool("DisableCaching");
    private static readonly bool DisableTokenCaching = GetBool("DisableTokenCaching");

    // OTHER
    public static readonly bool TalkativePerformance = GetBool("FastStart");
    public static readonly bool ProdPrepare = GetBool("ProdPrepare");
    



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
}