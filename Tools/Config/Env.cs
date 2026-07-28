using Scheder.Services.Database;

namespace Scheder.Config;

public class Env
{
    public static readonly string TelegramToken = Environment.GetEnvironmentVariable("TG_TOKEN")!;
    public static readonly string? WeatherApiToken = Environment.GetEnvironmentVariable("Weather_Token") ?? null;
    public static readonly long DebugUid = Environment.GetEnvironmentVariable("DebugUID") != null ? long.Parse(Environment.GetEnvironmentVariable("DebugUID")!) : 0;
    public static readonly string DB_HOST = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    public static readonly string DB_PORT = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    public static readonly string DB_NAME = Environment.GetEnvironmentVariable("DB_NAME") ?? DatabaseTools.DatabaseName;
    public static readonly string DB_USER = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    public static readonly string DB_PASS = Environment.GetEnvironmentVariable("DB_PASS") ?? "postgres";
    public static readonly string PreFetchData = Environment.GetEnvironmentVariable("PreFetchData") ?? "{}";
}