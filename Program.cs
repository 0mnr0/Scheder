using Scheder.TelegramInteractions.Helpers;
using Scheder.Tools.Config;


await RunConfig.test();
await BotRunner.PrepareMaterials();
await BotRunner.Once();
Console.WriteLine("\nBot started!");
await Task.Delay(Timeout.Infinite); // for docker


// TODO:
// - /notifyme
// - /daylistener
// - /dynamicmessage - for admins only 