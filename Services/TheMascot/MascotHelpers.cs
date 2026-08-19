namespace Scheder.Services.TheMascot;

public class MascotHelpers {
    
    public static bool IsEqualsTime(int hour, List<int> times) {
        return times.Contains(hour);
    } 
}