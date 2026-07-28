using System.Reflection;
using Scheder.Commands;
using Scheder.TelegramInteractions.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.Services.InterfacesAndHandlers;

public class CommandHandler
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly ITextHandler? _textHandler;

    public CommandHandler(ITextHandler? textHandler = null)
    {
        _textHandler = textHandler;
        LoadCommands();
    }

    private void LoadCommands()
    {
        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x =>
                typeof(ICommand).IsAssignableFrom(x) &&
                !x.IsInterface &&
                !x.IsAbstract);

        foreach (var type in commandTypes)
        {
            var attribute = type.GetCustomAttribute<CommandAttribute>();

            if (attribute == null)
                continue;

            var instance = (ICommand)Activator.CreateInstance(type)!;

            foreach (var name in attribute.Names)
            {
                _commands[name] = instance;
            }
        }
    }

    public async Task HandleAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        if (message.Text == null)
            return;

        var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return;

        var commandName = parts[0].Split('@')[0];
        
        if (!commandName.StartsWith('/') || !_commands.TryGetValue(commandName, out var command))
        {
            if (_textHandler != null)
                await _textHandler.HandleAsync(bot, message, cancellationToken);

            return;
        }

        var args = parts.Skip(1).ToArray();
        await command.ExecuteAsync(bot, message, args, cancellationToken);
    }
}