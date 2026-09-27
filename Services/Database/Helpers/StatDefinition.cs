// ReSharper disable InconsistentNaming
namespace Scheder.Services.Database.Helpers;

public abstract class StatDefinition {
    
    public const string AUTH_API_DOWN = "AUTH_API_DOWN";
    public const string FETCH_AUTH_API = "FETCH_AUTH_API";
    public const string FETCH_AUTH_API_FAIL = "FETCH_AUTH_API_FAIL";
    
    
    public const string FETCH_WEATHER_API = "FETCH_WEATHER_API";
    // Value: [int] response code of fetch result
    public const string FETCH_WEATHER_API_TIMEOUT = "FETCH_WEATHER_API_TIMEOUT";
    // Value: [str] "Timeout"

    public const string BOT_SCHED_ASK = "BOT_SCHED_ASK";
    public const string BOT_EXAM_ASK = "BOT_SCHED_ASK";
    public const string CODE_FAILURE = "CODE_FAILURE";

}