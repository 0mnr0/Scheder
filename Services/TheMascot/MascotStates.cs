namespace Scheder.Services.TheMascot;

public class MascotStates {
    public static class DailyActions {
        public static readonly string WannaSleep = "WannaSleep";
        public static readonly string Sleeping = "Sleeping";
        public static readonly string WakingUp = "WakingUp";
        public static readonly string Active = "Active";
        public static readonly string Tired = "Tired";
    }

    public static class TimeDividers {
        public static List<int> Night = [1, 2, 3, 4, 5, 6];
        public static List<int> Morning = [7, 8];
        public static List<int> ActiveDay = [9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20];
        public static List<int> Evening = [21, 22, 23];
    }

}