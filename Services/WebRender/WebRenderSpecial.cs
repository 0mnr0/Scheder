using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;
using Scheder.Services.Weather;
using Scheder.Tools;
using Scheder.Tools.Config;
using static Scheder.Tools.Logger;

namespace Scheder.Services.WebRender;

public class WebRenderSpecial {

    private class WebRenderTypes {
        public const int Background = 0;
        public const int AddAfter = 1;
    }
    private class WebRenderSelection {
        public const int Linear = 0;
        public const int Random = 1;
    }

    public class WebRenderData {
        public string[] TargetDates { get; set; } = [];
        public int ShowType { get; set; } = WebRenderTypes.Background;
        public bool IsContentInUrl { get; set; } = true;
        public int AddMax { get; set; } // Only if ShowType == AddAfter. Tell how many content allowing to add after.
        
        public bool AllowHue { get; set; }
        public bool ShowDither { get; set; } = true;
        public double? Brightness { get; set; } = 0.75;
        public double? Blur { get; set; } = 0;
        
        public string[] Content { get; set; } = []; // PATH or URL to content 
        public List<string> ReadyContent { get; set; } = []; 
        public int Selection { get; set; } = WebRenderSelection.Linear;
        public bool AutoFit { get; set; } = true;
        
        public WebRenderData Clone() => new() {
            TargetDates = (string[])TargetDates.Clone(),
            ShowType = ShowType,
            IsContentInUrl = IsContentInUrl,
            ShowDither = ShowDither,
            Brightness = Brightness,
            AllowHue = AllowHue,
            Blur = Blur,
            AddMax = AddMax,
            AutoFit = AutoFit,
            Content = (string[])Content.Clone(),
            ReadyContent = [..ReadyContent],
            Selection = Selection
        };
    }

    public class RenderMaterials {
        public required WebRenderData? Additional { get; set; }
        public required List<WeatherAPI.WeatherObject> Main { get; set; }
    }

    private static WebRenderData[] _rules = [];
    private static readonly HttpClient Client = new();


    public static async Task<List<byte[]>> RenderWeather(List<WeatherAPI.WeatherObject>? weatherData, PerformanceMetric? metric, BestDayOption.BestDayParseResult dayParse) {
        if (weatherData == null) return [];
        
        var day = DateTime.ParseExact(
            dayParse.StartDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture
        ).ToString("dd.MM.yyyy");
        
        var targetRule = GetRuleForDay(day);
        targetRule = LoadRule(targetRule);

        var setMaterials = new RenderMaterials {
            Main = weatherData,
            Additional = targetRule,
        };

        var weatherImages = await WebRender.RenderWeather(setMaterials, metric);
        return weatherImages;
    }

    private static WebRenderData? LoadRule(WebRenderData? rule) {
        if (rule is null) return null;
        
        var activeRule = rule.Clone();
        activeRule.AutoFit = activeRule.ShowType == WebRenderTypes.Background;
        var contentSelection = activeRule.Selection;
        var readyContent = activeRule.ReadyContent;

        if (contentSelection != WebRenderSelection.Random) return activeRule;
        
        readyContent = [.. readyContent.OrderBy(_ => Random.Shared.Next())];
        activeRule.ReadyContent = readyContent;
        return activeRule;
    }

    private static WebRenderData? GetRuleForDay(string date) {
        foreach (var rule in _rules) {
            var dates = rule.TargetDates;
            if (dates.Contains(date)) return rule;
        }

        return null;
    }


    public static async Task<string> DownloadImage(string url)
    {
        var uri = new Uri(url);

        var relativePath = uri.Host + uri.AbsolutePath.TrimStart('/');
        var filePath = Path.Combine(".files", relativePath);
        if (File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var data = await Client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(filePath, data);
        return filePath;
    }

    public static async Task Init() {
        if (string.IsNullOrEmpty(Env.WeatherSpec) || !Env.WeatherSpec.StartsWith('[')) return;
        _rules = JsonSerializer.Deserialize<WebRenderData[]>(Env.WeatherSpec) ?? [];

        if (_rules.Length > 0) {
            // Download Files
            Log.Information("[WebRules] Downloading all files...");
            Directory.CreateDirectory(".files");
            
            foreach (var rule in _rules) {
                if (!rule.IsContentInUrl) continue;
                
                var files = rule.Content;
                foreach (var file in files) {
                    var path = await DownloadImage(file);
                    path = Path.GetFullPath(path);
                    rule.ReadyContent.Add(path);
                }
            }
        }
    }


}