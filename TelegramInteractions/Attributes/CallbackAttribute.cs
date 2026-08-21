namespace Scheder.TelegramInteractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CallbackAttribute : Attribute
{
    public string[] Names { get; }
    public bool IgnoreSplitter { get; set; }

    public CallbackAttribute(params string[] names)
    {
        Names = names;
    }
}