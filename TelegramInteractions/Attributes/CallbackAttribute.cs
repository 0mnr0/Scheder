namespace Scheder.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CallbackAttribute : Attribute
{
    public string[] Names { get; }
    public bool IgnoreSplitter { get; set; } = false;

    public CallbackAttribute(params string[] names)
    {
        Names = names;
    }
}