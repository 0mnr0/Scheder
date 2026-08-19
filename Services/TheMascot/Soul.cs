namespace Scheder.Services.TheMascot;

public class Soul {
    private const int UpdateTimeCheck = 10;
    private static bool _onStartupTimeSkipped;
    private static string CurrentState;
    
    private static class Feelings {
        public static double Happiness = 66; // 0 - Bad Mood, 100 - Absolute happiness
        public static double Fatigue = 10; // Усталость
        public static double Apathy = 3; // Безразличие
        
        public static double GetSadness() => 100f-Happiness;
    }

    public static void Revive() {
        
        var thread = new Thread(void () =>
        {
            var lastMinute = -1;

            while (true)
            {
                var currentMinute = DateTime.Now.Minute;

                if (currentMinute % UpdateTimeCheck == 0 && currentMinute != lastMinute || !_onStartupTimeSkipped)
                {
                    _onStartupTimeSkipped = true;
                    lastMinute = currentMinute;
                    _ = OnEveryTick();
                }
                Thread.Sleep(1800);
            }
        }) {
            IsBackground = true
        };
        thread.Start();
    }


    private static async Task OnEveryTick() {
        var now = DateTime.Now;
        var hour = now.Hour;

        CurrentState = hour switch {
            0 => MascotStates.DailyActions.WannaSleep,
            1 or 2 or 3 or 4 or 5 or 6 => MascotStates.DailyActions.Sleeping,
            7 or 8 => MascotStates.DailyActions.WakingUp,
            9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 => MascotStates.DailyActions.Active,
            21 or 22 => MascotStates.DailyActions.Tired,
            23 => MascotStates.DailyActions.WannaSleep,
            _ => CurrentState
        };

        switch (CurrentState) {
            case "Sleeping":
                Feelings.Fatigue -= 3;
                Feelings.Happiness += 2.5;
                Feelings.Apathy -= 2;
                break;
            
            case "Active":
            case "WakingUp":
                Feelings.Apathy -= 0.5;
                break;
            
            case "WannaSleep":
                var angerPoints = Feelings.Apathy>1 ? 1d : 0d;
                Feelings.Apathy -= angerPoints;
                Feelings.Happiness -= angerPoints*2;
                break;
        }
    }
    
    
    public static void InsertEvent(int eventName) {
        var now = DateTime.Now;
        var hour = now.Hour;

        if (eventName == MascotEvents.AskedForSchedule || eventName == MascotEvents.AskedForExams) {
            if (MascotHelpers.IsEqualsTime(hour, MascotStates.TimeDividers.Night)) {
                Feelings.Apathy += 30;
                Feelings.Happiness -= 3;
            }
            else if (MascotHelpers.IsEqualsTime(hour, MascotStates.TimeDividers.Morning)) {
                Feelings.Fatigue += 3;
            }
        }
    }
    
    
}