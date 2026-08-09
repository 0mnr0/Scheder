using Microsoft.Extensions.Caching.Memory;
using Scheder.Services.Database;
using Scheder.TelegramInteractions.Commands.Settings.Data;
using Scheder.Tools;
using static Scheder.Tools.Logger;

namespace Scheder.Services.JournalAPI;

public class TokenService
{
    private const int JwtKeepTime = 29; // 29 минут
    

    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private static bool IsTimerExpired(DateTime dt)
    {
        return DateTime.Now >= dt.AddMinutes(JwtKeepTime);
    }

    public static async Task<(string?, string[])> Get(long uid, bool cacheUpdate = false,
        PerformanceMetric? metric = null, long parent = 0)
    {
        using (metric?.Measure(MetricType.TokenParse))
        {

            var allowCache = await SettingsService.GetBool(parent, SettingsList.AllowDataCaching, CancellationToken.None);
            
            if (allowCache && !cacheUpdate && Cache.TryGetValue(uid, out FetchedToken? cachedToken) && cachedToken != null &&
                cachedToken.Uid == uid)
            {
                Log.Information("[TokenService | {Uid}] Using cached token!", uid);
                return (cachedToken.Token, ["Cache"]);
            }

            if (!cacheUpdate)
            {
                Log.Information("[TokenService | {Uid}] No cached token, getting new!", uid);
            }



            var (token, lastUpdate) = await Memory.User.GetJWTAsync(uid);
            var isExpired = lastUpdate.HasValue && IsTimerExpired(lastUpdate.Value);


            Log.Information("[TokenService] Should parse new: {should}",
                string.IsNullOrEmpty(token) || lastUpdate == null || isExpired || allowCache);
            if (string.IsNullOrEmpty(token) || lastUpdate == null || isExpired || cacheUpdate || allowCache)
            {
                Log.Information(
                    "[TokenService] Cant find any fresh token, parsing new one... {ActualReasonForceCacheUpdate}",
                    cacheUpdate ? "(Actual reason: force cache update)" : allowCache ? "(Actual reason: forceNew)": "");
                var auth = await Memory.User.GetAuthAsync(uid);
                if (auth == null)
                {
                    return (null, []);
                }

                var (newToken, tries) = await API.GetTokenAsync(auth.Login, auth.Password);
                if (string.IsNullOrEmpty(newToken)) return (null, tries);

                var now = DateTime.Now;
                _ = Task.Run(async () => { await Memory.User.SetJWTAsync(uid, newToken); });

                var fetchedToken = new FetchedToken
                {
                    Uid = uid,
                    Token = newToken,
                    LastUpdate = now,
                    Tries = tries
                };

                Cache.Set(uid, fetchedToken, TimeSpan.FromMinutes(JwtKeepTime));
                return (newToken, tries);

            }


            var existingToken = new FetchedToken
            {
                Uid = uid,
                Token = token,
                LastUpdate = lastUpdate.Value,
                Tries = ["DBCache"]
            };


            var timeLeft = lastUpdate.Value.AddMinutes(JwtKeepTime) - DateTime.Now;
            if (timeLeft > TimeSpan.Zero)
            {
                Cache.Set(uid, existingToken, timeLeft);
            }

            return (token, ["DBCache"]);
        }
    }


    private class FetchedToken
    {
        public long Uid { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }
        public string[] Tries { get; set; } = [];
        public bool UsedBackup {get; set;} = false;
    }
}