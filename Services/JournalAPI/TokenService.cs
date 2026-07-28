using Microsoft.Extensions.Caching.Memory;
using Scheder.Services.Database;

namespace Scheder.Services.JournalAPI;

public class TokenService
{
    private const int JwtKeepTime = 29; // 29 минут
    

    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private static bool IsTimerExpired(DateTime dt)
    {
        return DateTime.Now >= dt.AddMinutes(JwtKeepTime);
    }

    public static async Task<string?> Get(long uid, bool cacheUpdate = false)
    {
        if (!cacheUpdate && Cache.TryGetValue(uid, out FetchedToken? cachedToken) && cachedToken != null && cachedToken.Uid == uid)
        {
            Console.WriteLine("[TokenService] Using cached token!");
            return cachedToken.Token;
        }

        if (!cacheUpdate)
        {
            Console.WriteLine("[TokenService] No cached token ");
        }



        var (token, lastUpdate) = await Memory.User.GetJWTAsync(uid);
        var isExpired = lastUpdate.HasValue && IsTimerExpired(lastUpdate.Value);
         
        
        Console.WriteLine("[TokenService] Should parse new: "+(string.IsNullOrEmpty(token) || lastUpdate == null || isExpired));
        if (string.IsNullOrEmpty(token) || lastUpdate == null || isExpired || cacheUpdate)
        {
            Console.WriteLine($"[TokenService] Cant find any fresh token, parsing new one... {(cacheUpdate ? "(Actual reason: force cache update)" : "")}");
            var auth = await Memory.User.GetAuthAsync(uid);
            if (auth == null)
            {
                 return null;
            }
            
            var newToken = await API.GetTokenAsync(auth.Login, auth.Password);
            if (string.IsNullOrEmpty(newToken)) return null;

            var now = DateTime.Now;
            _ = Task.Run(async () => { await Memory.User.SetJWTAsync(uid, newToken); });
                
            var fetchedToken = new FetchedToken
            {
                Uid = uid,
                Token = newToken,
                LastUpdate = now
            };
                
            Cache.Set(uid, fetchedToken, TimeSpan.FromMinutes(JwtKeepTime));
            return newToken;

        }
         
        
        var existingToken = new FetchedToken
        {
            Uid = uid,
            Token = token,
            LastUpdate = lastUpdate.Value
        };
        
        
        var timeLeft = lastUpdate.Value.AddMinutes(JwtKeepTime) - DateTime.Now;
        if (timeLeft > TimeSpan.Zero)
        {
            Cache.Set(uid, existingToken, timeLeft);
        }

        return token;
    }


    private class FetchedToken
    {
        public long Uid { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }
    }
}