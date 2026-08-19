using Scheder.Services.DateWatcher;
using Scheder.TelegramInteractions.Helpers;
using Scheder.Tools.Config;



await RunConfig.test();
await BotRunner.Prepare(); // creates a bot but doesn't start it yet 
await BotRunner.LoadMaterials();

DateWatcherService.Run();
BotRunner.Once();


Console.WriteLine("Bot Started!");
await Task.Delay(Timeout.Infinite); // for docker