using Microsoft.Playwright;
using Scheder.Services.Database;
using static Scheder.Tools.Logger;

namespace Scheder.Tools.Config;

public class RunConfig
{
    public static async Task Test()
    {
        
        DotNetEnv.Env.Load();
        Env.UpdateRules();
        
        if (Env.FastStart)
        {
            Log.Warning("[FastStart] .env test skipped! Not recommended for production purposes. ");
            return;
        }
        
        var isFatal = false;
        
        if (string.IsNullOrWhiteSpace(Env.TelegramToken))
        {
            isFatal = true;
            Log.Fatal("[TelegramToken] is null or empty!");
        }
        
        if (
            string.IsNullOrEmpty(Env.DbHost)
            || Env.DbPort is null || Env.DbPass == string.Empty
            || Env.DbName is null || Env.DbUser == string.Empty
            || Env.DbUser is null || Env.DbPass == string.Empty
            || Env.DbPass is null || Env.DbUser == string.Empty
            )
        {
            isFatal = true;
            Log.Fatal("[DB_AUTH] One of DB_* values is null or empty!");
        }
        
        if (!await DatabaseTools.DatabaseExists())
        {
            isFatal = true;
            Log.Fatal("[Database] Connection to database failed! Reason: \"{DbName}\" not exists!", Env.DbName);
            Log.Fatal(" -- Fix it with this command: CREATE DATABASE \"{DB_NAME}\"\n", Env.DbName);
        }
        
        if (!await IsPlaywrightAccessible() && !await PlaywrightInstalled())
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
        
        Log.Information("[DataBase] Database address = {hst}:{prt}",Env.DbHost, Env.DbPort);
    }


    private static Task<bool> PlaywrightInstalled()
    {
        try {
            Log.Debug("[Playwright] Playwright pre-install... (Requires even if will not be used)");
            var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
            return Task.FromResult(exitCode == 0);
        }
        catch (Exception) {
            return Task.FromResult(false);
        }
    }

    private static async Task<bool> IsPlaywrightAccessible()
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
            { 
                Headless = true 
            });
            await browser.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {   
            Console.WriteLine("FAILED: "+ex.Message);
            return false;
        }
    }
}