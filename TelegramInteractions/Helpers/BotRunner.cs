using Scheder.Config;
using Scheder.Services.ContextDetection;
using Scheder.Services.Database;
using Scheder.Services.InterfacesAndHandlers;
using Scheder.Services.JournalAPI.PreFetch;
using Scheder.Services.Weather;
using Scheder.Services.WebRender;
using Scheder.TelegramInteractions.Commands;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace Scheder.TelegramInteractions.Helpers;

public class BotRunner {
    
    public static async Task PrepareMaterials() {
        DotNetEnv.Env.Load();
        await Memory.InitializeAsync();
        ElevatedUserConfig.DebugUID = Env.DebugUid;

        RenderMaterialsExtractor.Extract();
        CachedScheduleLibrary.StartCleanupThread();
        CachedWeatherService.StartCleanupThread();

        await WebRender.EnsureInitializedAsync();
        DetectionContextRatio.InitEmbedded("Scheder.Services.ContextDetection.dataset.onnx");
        await PreFetchLoad.Run();

    }
    
    public static async Task Once() {
        var bot = new TelegramBotClient(Env.TelegramToken);
        var textHandler = new NonCommandSupport();
        var commandHandler = new CommandHandler(textHandler);
        var callbackInterface = new CallbackInterface();
        var updateHandler = new UpdateHandler(commandHandler, callbackInterface);

        
        await EphemeralCommand.AddAll(bot);
        bot.StartReceiving(
            updateHandler,
            new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
            });

    }
}