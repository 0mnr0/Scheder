using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Scheder.Tools;

public class Logger
{
    
    public static readonly ILogger Log = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();
    
}