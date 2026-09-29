namespace Scheder.Services.Weather;

public class WeatherObject(string time)
{
    public string Time { get; set; } = time;
    public double Temp { get; set; }
    public int Condition { get; set; }
    public string? WeatherTitle { get; set; }
    public string? WeatherIcon { get; set; }
    public string? WeatherTextIcon { get; set; }
}