using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Scheder.Tools;

public class Logger
{
    
    public static readonly string FileName = $"logs/app-run-{DateTime.Now:yyyyMMdd_HHmmss}.txt"; 
    
    public static readonly ILogger Log = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .WriteTo.File(FileName)
        .CreateLogger();
    
}