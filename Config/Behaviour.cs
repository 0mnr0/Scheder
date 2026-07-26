namespace Scheder.Config;

public abstract class Behaviour {
    
    public abstract class Groups {
        public const bool AllowWeatherImageOutput = true;
        public const bool AllowSendingResponseSpeed = true; // Sends response time to specific UID (as new ephemeral message (visible for this ID only))
    }
    
    public abstract class Users {
        public const bool AllowWeatherImageOutput = true;
        public const bool AllowSendingResponseSpeed = true; // Displays response times to specific UID (in message)
    }
    
}