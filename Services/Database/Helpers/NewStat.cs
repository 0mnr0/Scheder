namespace Scheder.Services.Database.Helpers;

public class NewStat {
    
    public static async Task OnNewStat(string filterType, string value) {
        var currentTime = DateTime.UtcNow;
        await Stats.InsertStatAsync(filterType, value, currentTime);
    }
    
    public static async Task<List<StatRecord>> GetStat(string filterType) {
        return await Stats.GetStatAsync(filterType);
    }
    
    // <summary> Use "dd-MM-yyyy" format </summary>
    public static async Task<List<StatRecord>> GetStat(string filterType, string date) {
        var stats = await GetStat(filterType);
        return [.. stats.Where(stat => stat.When.ToString("dd-MM-yyyy") == date)];
    }

    public static async Task<List<StatRecord>> GetStats() {
        return await Stats.GetStatsAsync();
    }
    
    // <summary> Use "dd-MM-yyyy" format </summary>
    public static async Task<List<StatRecord>> GetStats(string date) {
        var stats = await GetStats();
        return [.. stats.Where(stat => stat.When.ToString("dd-MM-yyyy") == date)];
    }
    
    
}