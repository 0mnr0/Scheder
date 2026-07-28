using Scheder.TelegramInteractions.Helpers;


await BotRunner.PrepareMaterials();
await BotRunner.Once();
Console.WriteLine("\nBot started!");
await Task.Delay(Timeout.Infinite); // for docker


// TODO:
// - /notifyme
// - /exams
// - /settings - Ephemeral, for admins only
// - /daylistener
// - /dynamicmessage - for admins only 