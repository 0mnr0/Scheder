using Scheder.Services.DateWatcher;
using Scheder.TelegramInteractions.Helpers;
using Scheder.Tools.Config;



await RunConfig.Test();
await BotRunner.Prepare(); // creates a bot but doesn't start it yet 
await BotRunner.LoadMaterials();

DateWatcherService.Run();
BotRunner.Once();

Console.WriteLine("Bot Started!");
await Task.Delay(Timeout.Infinite); // for docker


// TODO:
// Сделать проверку на корректность расположенных файлов для WebSpecial (ContentType: local)
// Исправить кэш изображений погода (кэшируется один день на все ответы)
// Переехать на dd_mm_yyyy в BestDay (str)