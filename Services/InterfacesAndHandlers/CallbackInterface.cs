using System.Reflection;
using Scheder.TelegramInteractions.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Scheder.Services.InterfacesAndHandlers;

public class CallbackInterface
{
    // Точные совпадения: [Callback("bindgroup:run", IgnoreSplitter = true)]
    private readonly Dictionary<string, ICallbackCommand> _exactCallbacks = new();

    // Обычные, с разбором по ':' -> имя + аргументы
    private readonly Dictionary<string, ICallbackCommand> _splitCallbacks = new();

    public CallbackInterface()
    {
        LoadCallbacks();
    }

    private void LoadCallbacks()
    {
        var callbackTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x =>
                typeof(ICallbackCommand).IsAssignableFrom(x) &&
                !x.IsInterface &&
                !x.IsAbstract);

        foreach (var type in callbackTypes)
        {
            var attribute = type.GetCustomAttribute<CallbackAttribute>();

            if (attribute == null)
                continue;

            var instance = (ICallbackCommand)Activator.CreateInstance(type)!;
            var target = attribute.IgnoreSplitter ? _exactCallbacks : _splitCallbacks;

            foreach (var name in attribute.Names)
            {
                target[name] = instance;
            }
        }
    }

    public async Task HandleCallbackAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.CallbackQuery || update.CallbackQuery?.Data == null)
            return;

        var callbackQuery = update.CallbackQuery;
        var data = callbackQuery.Data;
        
        if (_exactCallbacks.TryGetValue(data, out var exactCallback))
        {
            await exactCallback.ExecuteAsync(bot, callbackQuery, Array.Empty<string>(), cancellationToken);
            return;
        }
        
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return;

        var callbackName = parts[0];

        if (_splitCallbacks.TryGetValue(callbackName, out var callback))
        {
            var args = parts.Skip(1).ToArray();
            await callback.ExecuteAsync(bot, callbackQuery, args, cancellationToken);
        }
    }
}