using Scheder.Config;

namespace Scheder.Services.JournalAPI.PreFetch;

public class PreFetchLoad {
    public static async Task Run() {
        var prefetchJson = Env.PreFetchData;
        Console.WriteLine($"Pre-fetch JSON: {prefetchJson}");
        if (string.IsNullOrEmpty(prefetchJson) || prefetchJson.Length == 0 || prefetchJson == "{}") {return;}
        var config = TokenPreFetch.ParsePrefetchConfig(prefetchJson);
        TokenPreFetch.SetFetchList(config);
        await TokenPreFetch.ForceUpdateAll();
        TokenPreFetch.RunService();
    }
}