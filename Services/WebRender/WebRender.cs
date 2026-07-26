using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace Scheder.Services.WebRender;

public class WebRender
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    
    public static async Task EnsureInitializedAsync()
    {
        if (_browser is not null) return;

        await Lock.WaitAsync();
        try
        {
            if (_browser is not null) return;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Args = new[]
                {
                    "--disable-web-security",
                    "--allow-file-access-from-files",
                    "--disable-site-isolation-trials"
                },
                Headless = false
            });
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task<IPage> OpenLocalFileAsync(string filePath, bool waitForNetworkIdle = false)
    {
        await EnsureInitializedAsync();

        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        var fullPath = Path.GetFullPath(filePath);
        var fileUrl = new Uri(fullPath).AbsoluteUri;

        
        if (waitForNetworkIdle)
        {
            await page.GotoAsync(fileUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
        }
        else
        {
            await page.GotoAsync(fileUrl);
        }

        return page;
    }


    
    public static async Task<List<byte[]>> RenderWeather(List<WeatherAPI.WeatherObject> weather, Stopwatch? timer)
    {
        
        var dir = RenderMaterialsExtractor.Extract();
        var indexPath = Path.Combine(dir, "weather.html");
        
        var page = await OpenLocalFileAsync(indexPath, true);
        await page.EvaluateAsync(
            @"runProd()",
            null
        );
        await page.EvaluateAsync(
            @"weather => updateWeather(weather)",
            weather
        );
        
        var element = page.Locator("#FirstTarget");
        var firstImage = await element.ScreenshotAsync(new LocatorScreenshotOptions
        {
            OmitBackground = true
        });
        
        element = page.Locator("#SecondTarget");
        var secondImage = await element.ScreenshotAsync(new LocatorScreenshotOptions
        {
            OmitBackground = true,
            Type=ScreenshotType.Png
        });
        
        timer?.Stop();
        return [firstImage, secondImage];
    }

    
    public static async Task ShutdownAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;
    }
}