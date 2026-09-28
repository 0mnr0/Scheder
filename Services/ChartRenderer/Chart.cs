using SkiaSharp;

namespace Scheder.Services.ChartRenderer;

/// <summary>
/// График "минуты суток (00:00-24:00) / количество событий" с поддержкой
/// нескольких линий, каждая со своими настройками.
///
/// Использование:
///
///   var chart = new Chart
///   {
///       Background = SKColors.White,
///       Foreground = new SKColor(90, 90, 90),
///       Title = "События за день",
///       GridHours = 1
///   };
///
///   var chartLines = new List&lt;ChartLine&gt;
///   {
///       ChartLine.FromEvents(stats1, "Ошибки", new SKColor(220, 53, 69)),
///       ChartLine.FromEvents(stats2, "Запросы", new SKColor(37, 99, 235))
///   };
///
///   chart.SetChartLines(chartLines);
///   byte[] image = chart.Render();
/// </summary>
public class Chart
{
    // ---- Layout ----
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 500;
    public int PaddingLeft { get; set; } = 60;
    public int PaddingRight { get; set; } = 30;
    public int PaddingTop { get; set; } = 30;
    public int PaddingBottom { get; set; } = 50;

    // ---- Стиль ----
    public SKColor Background { get; set; } = SKColors.White;
    public SKColor Foreground { get; set; } = new SKColor(90, 90, 90); // цвет текста/подписей
    public SKColor GridLineColor { get; set; } = new SKColor(230, 230, 230);
    public string FontFamily { get; set; } = "DejaVu Sans";
    public string Title { get; set; } = "";

    // ---- Сетка ----
    /// <summary>Шаг вертикальной сетки/подписей времени в часах. По умолчанию — каждый час.</summary>
    public int GridHours { get; set; } = 1;

    /// <summary>Показывать ли горизонтальные линии сетки (по значению). Подписи значений рисуются всегда.</summary>
    public bool ShowHorizontalGrid { get; set; } = true;

    /// <summary>
    /// Показывать ли самую правую вертикальную линию сетки (24:00) — по сути,
    /// правую границу графика. Подпись "24:00" рисуется в любом случае.
    /// </summary>
    public bool ShowRightBorder { get; set; } = true;

    /// <summary>Количество горизонтальных линий сетки (по значению), даже если сами линии скрыты.</summary>
    public int HorizontalGridLines { get; set; } = 5;

    // ---- Легенда ----
    public bool ShowLegend { get; set; } = true;

    private readonly List<ChartLine> _lines = new();

    public void SetChartLines(IEnumerable<ChartLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
    }

    public void AddChartLine(ChartLine line) => _lines.Add(line);

    /// <summary>Быстрое применение готовой светлой/тёмной палитры поверх текущих настроек.</summary>
    public void ApplyTheme(ChartTheme theme)
    {
        var c = ChartThemeColors.Get(theme);
        Background = c.Background;
        Foreground = c.AxisText;
        GridLineColor = c.GridLine;
    }

    public byte[] Render()
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(Background);

        float plotLeft = PaddingLeft;
        float plotRight = Width - PaddingRight;
        float plotTop = PaddingTop + (string.IsNullOrEmpty(Title) ? 0 : 30);
        float plotBottom = Height - PaddingBottom - (ShowLegend && _lines.Any(l => !string.IsNullOrEmpty(l.Title)) ? 24 : 0);
        float plotWidth = plotRight - plotLeft;
        float plotHeight = plotBottom - plotTop;

        var typeface = SKTypeface.FromFamilyName(FontFamily, SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        using var font = new SKFont(typeface, 12);
        using var titleFont = new SKFont(typeface, 20);
        using var textPaint = new SKPaint { Color = Foreground, IsAntialias = true };
        using var gridPaint = new SKPaint { Color = GridLineColor, StrokeWidth = 1, IsAntialias = true };

        if (!string.IsNullOrEmpty(Title))
            canvas.DrawText(Title, plotLeft, PaddingTop, SKTextAlign.Left, titleFont, textPaint);

        // ---- Вертикальная сетка: каждый GridHours часов, от 00:00 до 24:00 ----
        for (int hour = 0; hour <= 24; hour += GridHours)
        {
            float frac = hour / 24f;
            float x = plotLeft + frac * plotWidth;

            bool isRightEdge = hour >= 24;
            if (!isRightEdge || ShowRightBorder)
                canvas.DrawLine(x, plotTop, x, plotBottom, gridPaint);

            string label = $"{hour % 24:00}:00";
            float w = font.MeasureText(label, textPaint);
            canvas.DrawText(label, x - w / 2, plotBottom + 20, font, textPaint);
        }

        // ---- Общий диапазон значений по всем линиям (кол-во событий всегда >= 0) ----
        double maxVal = 1;
        foreach (var line in _lines)
        {
            if (line.Values.Length == 0) continue;
            double m = line.Values.Max();
            if (m > maxVal) maxVal = m;
        }
        maxVal = NiceCeiling(maxVal);
        const double minVal = 0;

        float YForValue(double v)
        {
            double frac = (v - minVal) / (maxVal - minVal);
            return (float)(plotBottom - frac * plotHeight);
        }

        // ---- Горизонтальная сетка ----
        for (int i = 0; i <= HorizontalGridLines; i++)
        {
            double v = minVal + (maxVal - minVal) * i / HorizontalGridLines;
            float y = YForValue(v);

            if (ShowHorizontalGrid)
                canvas.DrawLine(plotLeft, y, plotRight, y, gridPaint);

            string label = v.ToString("0.##");
            float w = font.MeasureText(label, textPaint);
            canvas.DrawText(label, plotLeft - w - 10, y + 4, font, textPaint);
        }

        // ---- Линии данных ----
        foreach (var line in _lines)
            DrawLine(canvas, line, plotLeft, plotWidth, plotBottom, YForValue);

        // ---- Легенда ----
        if (ShowLegend)
            DrawLegend(canvas, font, textPaint, plotLeft, plotBottom + 40);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawLine(SKCanvas canvas, ChartLine line, float plotLeft, float plotWidth,
        float plotBottom, Func<double, float> yForValue)
    {
        int n = line.Values.Length;
        if (n == 0) return;

        var pts = new SKPoint[n];
        for (int i = 0; i < n; i++)
        {
            // i пробегает минуты 0..1439, весь диапазон соответствует 00:00-24:00
            float x = plotLeft + (i / (float)n) * plotWidth;
            float y = yForValue(line.Values[i]);
            pts[i] = new SKPoint(x, y);
        }

        using var path = new SKPath();
        using var fillPath = new SKPath();

        path.MoveTo(pts[0]);
        fillPath.MoveTo(pts[0].X, plotBottom);
        fillPath.LineTo(pts[0]);

        if (line.Smooth && pts.Length > 2)
        {
            // Catmull-Rom -> кубическая кривая Безье, даёт сглаженный/скруглённый график
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var p0 = pts[Math.Max(i - 1, 0)];
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = pts[Math.Min(i + 2, pts.Length - 1)];

                var cp1 = new SKPoint(p1.X + (p2.X - p0.X) / 6f, p1.Y + (p2.Y - p0.Y) / 6f);
                var cp2 = new SKPoint(p2.X - (p3.X - p1.X) / 6f, p2.Y - (p3.Y - p1.Y) / 6f);

                // Ограничиваем контрольные точки диапазоном соседних значений — иначе на
                // резком одиночном "шипе" сплайн проскакивает за пределы значений (в т.ч.
                // уходит ниже нуля) — классический артефакт Catmull-Rom на выбросах.
                float cp1YMin = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
                float cp1YMax = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));
                cp1.Y = Math.Clamp(cp1.Y, cp1YMin, cp1YMax);

                float cp2YMin = Math.Min(p1.Y, Math.Min(p2.Y, p3.Y));
                float cp2YMax = Math.Max(p1.Y, Math.Max(p2.Y, p3.Y));
                cp2.Y = Math.Clamp(cp2.Y, cp2YMin, cp2YMax);

                path.CubicTo(cp1, cp2, p2);
                fillPath.CubicTo(cp1, cp2, p2);
            }
        }
        else
        {
            for (int i = 1; i < pts.Length; i++)
            {
                path.LineTo(pts[i]);
                fillPath.LineTo(pts[i]);
            }
        }

        fillPath.LineTo(pts[^1].X, plotBottom);
        fillPath.Close();

        if (line.FillArea)
        {
            using var fillPaint = new SKPaint
            {
                Color = line.Color.WithAlpha(line.FillAlpha),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawPath(fillPath, fillPaint);
        }

        using var linePaint = new SKPaint
        {
            Color = line.Color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = line.LineWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawPath(path, linePaint);
    }

    private void DrawLegend(SKCanvas canvas, SKFont font, SKPaint textPaint, float startX, float y)
    {
        float x = startX;
        const float swatchSize = 12f;
        const float gap = 8f;
        const float betweenItems = 24f;

        foreach (var line in _lines)
        {
            if (string.IsNullOrEmpty(line.Title)) continue;

            using var swatchPaint = new SKPaint { Color = line.Color, IsAntialias = true };
            canvas.DrawRect(SKRect.Create(x, y - swatchSize, swatchSize, swatchSize), swatchPaint);

            float textX = x + swatchSize + gap;
            canvas.DrawText(line.Title, textX, y, font, textPaint);

            float w = font.MeasureText(line.Title, textPaint);
            x = textX + w + betweenItems;
        }
    }

    /// <summary>Округление верхней границы шкалы значений до "красивого" числа (1/2/5 * 10^n).</summary>
    private static double NiceCeiling(double value)
    {
        if (value <= 0) return 1;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        double residual = value / magnitude;
        double niceResidual = residual switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            _ => 10
        };
        return niceResidual * magnitude;
    }
}