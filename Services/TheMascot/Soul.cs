namespace Scheder.Services.TheMascot;

public class Soul {
    private const int UpdateTimeCheck = 2;
    private static bool _onStartupTimeSkipped;
    
    private static class Feelings {
        public static double Fatigue = 0; // Усталость
        public static double Apathy = 0; // Безразличие
        
        
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

    private async static Task OnEveryTick() {
        
    }


}