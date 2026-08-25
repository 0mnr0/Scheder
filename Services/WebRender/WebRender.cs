using System.Text.Json;
using Microsoft.Playwright;
using Scheder.Tools;

namespace Scheder.Services.WebRender;
public class WebRender
{
    private static readonly MetricType Metric = MetricType.WeatherRender;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static IBrowserContext? _context;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    
    private static Task<IPage>? _pendingWeatherPage;
    private static readonly Lock PendingPageGate = new();

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
                Args = [
                    "--disable-web-security",
                    "--allow-file-access-from-files",
                    "--disable-site-isolation-trials"
                ],
                Headless = false
            });
            
            _context = await _browser.NewContextAsync();
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task<IPage> OpenLocalFileAsync(string filePath, bool waitForNetworkIdle = false)
    {
        await EnsureInitializedAsync();

        var page = await _context!.NewPageAsync();

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

    private static async Task<IPage> PrepareWeatherPageAsync()
    {
        var dir = RenderMaterialsExtractor.Extract();
        var indexPath = Path.Combine(dir, "weather.html");

        var page = await OpenLocalFileAsync(indexPath, true);
        await page.EvaluateAsync("runProd()");
        return page;
    }

    private static Task<IPage> RentWeatherPageAsync()
    {
        Task<IPage> task;
        lock (PendingPageGate)
        {
            task = _pendingWeatherPage ??= PrepareWeatherPageAsync();
            _pendingWeatherPage = null;
        }
        return task;
    }
    
    private static void ScheduleNextWeatherPage()
    {
        lock (PendingPageGate)
        {
            _pendingWeatherPage ??= PrepareWeatherPageAsync();
        }
    }
    
    public static void PrewarmWeatherPage() => ScheduleNextWeatherPage();

    public static async Task<List<byte[]>> RenderWeather(WebRenderSpecial.RenderMaterials weather, PerformanceMetric? metric)
    {
        using (metric?.Measure(Metric)) {
            var page = await RentWeatherPageAsync();
            try {
                var additionalBlocks 
                    = await page.EvaluateAsync<string[]>(@"async (weather) => { return await updateWeather(weather) }", weather);

                List<byte[]> screenShots = [];
                var element = page.Locator("#FirstTarget");
                var firstImage = await element.ScreenshotAsync(new LocatorScreenshotOptions {
                    OmitBackground = true
                });
                screenShots.Add(firstImage);

                element = page.Locator("#SecondTarget");
                var secondImage = await element.ScreenshotAsync(new LocatorScreenshotOptions {
                    OmitBackground = true,
                    Type = ScreenshotType.Png
                });
                screenShots.Add(secondImage);
                
                foreach (var block in additionalBlocks) {
                    var specElement = page.Locator(block);
                    var img = await specElement.ScreenshotAsync(new LocatorScreenshotOptions {
                        OmitBackground = true,
                        Type = ScreenshotType.Png
                    });

                    if (img.Length == 0) { continue; }
                    screenShots.Add(img);
                }
                
                return screenShots;
            }
            finally {
                //await page.CloseAsync();
                ScheduleNextWeatherPage();
            }
        }
    }

    public static async Task ShutdownAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
            _context = null;
        }
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;

        lock (PendingPageGate)
        {
            _pendingWeatherPage = null;
        }
    }
}