using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Scheder.Tools;

public class Logger
{
    
    public static readonly ILogger Log = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .WriteTo.File($"logs/app-{DateTime.Now:yyyyMMdd_HHmmss}.txt")
        .CreateLogger();
    
}