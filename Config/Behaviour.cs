namespace Scheder.Config;

public abstract class Behaviour {
    
    public abstract class Groups {
        public static readonly bool AllowWeatherImageOutput = true;
        public static readonly bool AllowSendingResponseSpeed = true; // Sends response time to specific UID (as new ephemeral message (visible for this ID only))
    }
    
    public abstract class Users {
        public static readonly bool AllowWeatherImageOutput = true;
        public static readonly bool AllowSendingResponseSpeed = true; // Displays response times to specific UID (in message)
    }
    
}