using Microsoft.Playwright;
using Scheder.Services.Database;
using static Scheder.Tools.Logger;

namespace Scheder.Tools.Config;

public class RunConfig
{
    public static async Task test()
    {
        
        DotNetEnv.Env.Load();
        var isFatal = false;
        
        if (string.IsNullOrWhiteSpace(Env.TelegramToken))
        {
            isFatal = true;
            Log.Fatal("[TelegramToken] is null or empty!");
        }
        
        if (
            string.IsNullOrEmpty(Env.DB_HOST)
            || Env.DB_PORT is null || Env.DB_PASS == string.Empty
            || Env.DB_NAME is null || Env.DB_USER == string.Empty
            || Env.DB_USER is null || Env.DB_PASS == string.Empty
            || Env.DB_PASS is null || Env.DB_USER == string.Empty
            )
        {
            isFatal = true;
            Log.Fatal("[DB_AUTH] One of DB_* values is null or empty!");
        }
        
        if (!await DatabaseTools.DatabaseExists())
        {
            isFatal = true;
            Log.Fatal("[Database] Connection to database failed! Reason: \"{DbName}\" not exists!", Env.DB_NAME);
            Log.Fatal(" -- Fix it with this command: CREATE DATABASE \"{DB_NAME}\"\n", Env.DB_NAME);
        }
        
        if (!await PlaywrightInstalled())
        {
            isFatal = true;
            Log.Fatal("[Playwright] Playwright is failed to start, please install it! (Even if you disabled weather)");
        }

        
        
        if (Env.DebugUid == 0)
        {
            Log.Information("[DebugUid] is empty, debug data will not be sent");
        }

        if (string.IsNullOrWhiteSpace(Env.WeatherApiToken))
        {
            Log.Warning("[WeatherApiToken] is null or empty!");
        }

        if (!isFatal)
        {
            Log.Warning("[RunConfigTest] .env is fine, launching...");
        } else
        {
            throw new Exception("Bot is not ready to start!");
        }
    }


    private static Task<bool> PlaywrightInstalled()
    {
        Log.Debug("[Playwright] Playwright pre-install... (Requires even if will not be used)");
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        return Task.FromResult(exitCode == 0);
    }
}