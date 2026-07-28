using Scheder.Callbacks;
using Scheder.ClientSideCommandFix;
using Scheder.Commands;
using Scheder.Config;
using Scheder.ContextDetection;
using Scheder.JournalAPI;
using Scheder.Services;
using Scheder.Services.WebRender;
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