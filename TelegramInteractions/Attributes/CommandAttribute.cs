namespace Scheder.TelegramInteractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandAttribute : Attribute
{
    public string[] Names { get; }

    public CommandAttribute(params string[] names)
    {
        if (names == null || names.Length == 0)
            throw new ArgumentException("Command must have at least one name.");

        Names = names;
    }
}