using SkiaSharp;

namespace Scheder.Services.ChartRenderer;

public enum ChartTheme { Light, Dark }
public class ChartThemeColors
{
    public SKColor Background;
    public SKColor GridLine;
    public SKColor AxisText;
    public SKColor LineColor;
    public SKColor FillColor;
    public SKColor PointColor;
 
    public static ChartThemeColors Get(ChartTheme theme) => theme switch
    {
        ChartTheme.Dark => new ChartThemeColors
        {
            Background = new SKColor(24, 24, 28),
            GridLine   = new SKColor(55, 55, 60),
            AxisText   = new SKColor(200, 200, 205),
            LineColor  = new SKColor(88, 166, 255),
            FillColor  = new SKColor(88, 166, 255, 40),
            PointColor = new SKColor(88, 166, 255)
        },
        _ => new ChartThemeColors
        {
            Background = SKColors.White,
            GridLine   = new SKColor(230, 230, 230),
            AxisText   = new SKColor(90, 90, 90),
            LineColor  = new SKColor(37, 99, 235),
            FillColor  = new SKColor(37, 99, 235, 30),
            PointColor = new SKColor(37, 99, 235)
        }
    };
}