namespace Scheder.Tools.Config;

public abstract class Behaviour {
    
    public abstract class Groups {
        public static readonly bool AllowWeatherImageOutput = true;
        public static readonly bool AllowSendingResponseSpeed = true; // Sends response time to specific UID (as new ephemeral message (visible for this ID only))
    }
    
    public abstract class Users {
        public static readonly bool AllowWeatherImageOutput = true;
        public static readonly bool AllowSendingResponseSpeed = true; // Displays response times to specific UID (in message)
        public static readonly bool AllowDisplaySpeedMetricToAnyone = true; // anyone can see speed metric in pm
    }

    public abstract class Other {
        public static readonly bool AllowPreFetch = true;
        
        public static readonly bool AllowScheduleCaching = true; // enable caching system for schedule
        public static readonly bool AllowWeatherCaching = true; // enable caching system for weather
        
        public static readonly int ScheduleCachingTime = 20; // [20 Minutes] How long is the schedule cache considered relevant
        public static readonly int WeatherCachingTime = 20; // [20 Minutes] How long is the weather cache considered relevant
    }


    public static readonly string[] NonRegisteredUserCanInteractOnlyWithThisCommands = ["/start", "/auth"];
}