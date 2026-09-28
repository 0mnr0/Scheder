using Scheder.Services.Database.Helpers;
using SkiaSharp;

namespace Scheder.Services.ChartRenderer;

public class GraphCollector {
    
    
    public static async Task ApiStat() {
        
        var chart = new Chart {
            Width = 2000,
            ShowHorizontalGrid = false,
            ShowRightBorder = false,
            Background = SKColor.Parse("#29100B"), // 12, 59, 13
            Foreground = SKColor.Parse("#FFAB99"), 
            Title = "События за день",
            GridHours = 1
        };
        
        List<ChartLine> chartLines = [];
        var statList = await NewStat.GetStat(StatDefinition.FETCH_AUTH_API);
        var line = ChartLine.FromEvents(statList, "Ошибки", new SKColor(220, 53, 69));
        line.SpreadOverMinutes(0);
        chartLines.Add(line);
        
        chart.SetChartLines(chartLines);
        var graph = chart.Render();
        
        const string filePath = @"C:\ProgramData\chartOutput.png";
        await File.WriteAllBytesAsync(filePath, graph);


    }
}