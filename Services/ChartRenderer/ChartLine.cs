using Scheder.Services.Database;
using SkiaSharp;

namespace Scheder.Services.ChartRenderer;

/// <summary>
/// Одна линия на графике: поминутные данные за сутки + собственные настройки отображения.
/// </summary>
public class ChartLine
{
    public const int MinutesPerDay = 24 * 60; // 1440

    /// <summary>
    /// Поминутные значения за сутки. Length == MinutesPerDay.
    /// Index 0 = 00:00, index 1439 = 23:59.
    /// </summary>
    public double[] Values { get; set; } = new double[MinutesPerDay];

    public SKColor Color { get; set; } = new SKColor(37, 99, 235);
    public string Title { get; set; } = "";

    public bool Smooth { get; set; } = true;
    public bool FillArea { get; set; } = false;
    public float LineWidth { get; set; } = 2.5f;

    /// <summary>Прозрачность заливки под кривой (0-255), используется если FillArea == true.</summary>
    public byte FillAlpha { get; set; } = 30;

    /// <summary>
    /// Строит линию как КОЛИЧЕСТВО событий в каждую минуту суток.
    /// Записи группируются по времени (HH:mm) без учёта даты — данные за
    /// разные дни суммируются в одну 24-часовую шкалу.
    /// </summary>
    public static ChartLine FromEvents(IEnumerable<StatRecord> stats, string title, SKColor color, int spread=0)
    {
        var values = new double[MinutesPerDay];

        foreach (var s in stats)
        {
            int minuteOfDay = s.When.Hour * 60 + s.When.Minute;
            if (minuteOfDay < 0 || minuteOfDay >= MinutesPerDay)
                continue;

            values[minuteOfDay]++;
        }

        return new ChartLine
        {
            Values = values,
            Title = title,
            Color = color,
            
        };
    }

    /// <summary>
    /// Альтернативный вариант: не количество событий, а сумма числовых Value
    /// за каждую минуту (на случай если понадобится вместо счётчика).
    /// Записи с нечисловым Value игнорируются.
    /// </summary>
    public static ChartLine FromValueSum(IEnumerable<StatRecord> stats, string title, SKColor color)
    {
        var values = new double[MinutesPerDay];

        foreach (var s in stats)
        {
            if (!double.TryParse(s.Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v))
                continue;

            int minuteOfDay = s.When.Hour * 60 + s.When.Minute;
            if (minuteOfDay < 0 || minuteOfDay >= MinutesPerDay)
                continue;

            values[minuteOfDay] += v;
        }

        return new ChartLine
        {
            Values = values,
            Title = title,
            Color = color
        };
    }

    /// <summary>
    /// Размазывает каждое значение по соседним минутам гауссовым ядром — резкий
    /// одноминутный всплеск превращается в плавный "холмик" на несколько минут
    /// вокруг него. Сутки считаются цикличными (23:59 и 00:00 — соседние минуты).
    /// Суммарное количество событий (площадь под графиком) сохраняется.
    /// </summary>
    /// <param name="radiusMinutes">
    /// На сколько минут в каждую сторону "растекается" всплеск. 2-5 обычно достаточно
    /// для одноминутных данных; 0 или меньше — метод ничего не делает.
    /// </param>
    public void SpreadOverMinutes(int radiusMinutes)
    {
        if (radiusMinutes <= 0) return;

        int n = Values.Length;
        var kernel = BuildGaussianKernel(radiusMinutes);
        int kr = kernel.Length / 2;
        var result = new double[n];

        for (int i = 0; i < n; i++)
        {
            double sum = 0;
            for (int k = -kr; k <= kr; k++)
            {
                int j = ((i + k) % n + n) % n; // циклический индекс по суткам
                sum += Values[j] * kernel[k + kr];
            }
            result[i] = sum;
        }

        Values = result;
    }

    private static double[] BuildGaussianKernel(int radius)
    {
        int size = radius * 2 + 1;
        var kernel = new double[size];
        double sigma = Math.Max(radius / 2.0, 0.5);
        double sum = 0;

        for (int i = 0; i < size; i++)
        {
            double x = i - radius;
            kernel[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
            sum += kernel[i];
        }

        for (int i = 0; i < size; i++)
            kernel[i] /= sum; // нормализация, чтобы сумма значений не "поплыла"

        return kernel;
    }
}