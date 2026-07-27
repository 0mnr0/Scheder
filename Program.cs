using Scheder.Callbacks;
using Scheder.ClientSideCommandFix;
using Scheder.Commands;
using Scheder.Config;
using Scheder.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Scheder.ContextDetection;
using Scheder.JournalAPI;
using Scheder.Services.WebRender;
using Telegram.Bot.Types.Enums;

using System.Reflection;
using System.Runtime.Versioning;

DotNetEnv.Env.Load();
await Memory.InitializeAsync();
ElevatedUserConfig.DebugUID = Env.DebugUid;

var bot = new TelegramBotClient(Env.TelegramToken);
var textHandler = new NonCommandSupport();
var commandHandler = new CommandHandler(textHandler);
var callbackInterface = new CallbackInterface();
var updateHandler = new UpdateHandler(commandHandler, callbackInterface);

RenderMaterialsExtractor.Extract();
CachedScheduleLibrary.StartCleanupThread();
CachedWeatherService.StartCleanupThread();

await WebRender.EnsureInitializedAsync();

await EphemeralCommand.AddAll(bot);
bot.StartReceiving(
    updateHandler,
    new ReceiverOptions
    {
        AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
    });

DetectionContextRatio.InitEmbedded("Scheder.ContextDetection.dataset.onnx");



var framework = Assembly.GetEntryAssembly()?
    .GetCustomAttribute<TargetFrameworkAttribute>()?
    .FrameworkName;

Console.WriteLine(framework);
await PreFetchLoad.Run();

Console.WriteLine("\nBot started.");
await Task.Delay(Timeout.Infinite);


// TODO:
// - Добавить кнопки для переключения дней до двух часов ночи
// - /notifyme
// - /exams
// - /settings - Ephemeral, for admins only
// - /daylistener
// - /dynamicmessage - for admins only 