using Scheder.Services.ContextDetection;
using Scheder.Services.Database;
using Scheder.Services.DateWatcher;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI.PreFetch;
using Scheder.Services.Weather;
using Scheder.Services.WebRender;
using Scheder.TelegramInteractions.Commands;
using Scheder.TelegramInteractions.Commands.Other;
using Scheder.Tools.Config;
using Scheder.Tools.Proxy;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using static Scheder.Tools.Logger;

namespace Scheder.TelegramInteractions.Helpers;

public class BotRunner {
    private static TelegramBotClient _bot = new(Env.TelegramToken!);
    private static UpdateHandler _updateHandler = new(new CommandHandler(), new CallbackInterface());
    
    private static async Task CookMaterials() {
        DotNetEnv.Env.Load();
        await Memory.InitializeAsync();
        ElevatedUserConfig.DebugUID = Env.DebugUid;

        RenderMaterialsExtractor.Extract();
        CachedScheduleLibrary.StartCleanupThread();
        CachedWeatherService.StartCleanupThread();

        await WebRender.EnsureInitializedAsync();
        WebRender.PrewarmWeatherPage();
        DetectionContextRatio.InitEmbedded("Scheder.Services.ContextDetection.dataset.onnx");
        await PreFetchLoad.Run();
        Log.Information("Materials is ready, bot can run perfectly prepared now!");
    }

    public static async Task LoadMaterials()
    {
        var task = CookMaterials();
        if (!Env.FastStart)
        {
            await task;
        }
    }
    
    public static async Task Prepare() {
        var proxyClient = Proxy.SetAutoProxy();
        
        _bot = new TelegramBotClient(Env.TelegramToken!, httpClient: proxyClient);

        var textHandler = new NonCommandSupport();
        var commandHandler = new CommandHandler(textHandler);
        var callbackInterface = new CallbackInterface();
        _updateHandler = new UpdateHandler(commandHandler, callbackInterface);
        DateWatcherService.Load(_bot);

        var cmdPatch = EphemeralCommand.AddAll(_bot);
        if (!Env.FastStart)
        {
            await cmdPatch;
        }
        
    }
    
    public static void Once() {
        _bot.StartReceiving(
            _updateHandler,
            new ReceiverOptions {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
            });
    }
}