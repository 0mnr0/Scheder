using System.Text;
using System.Text.RegularExpressions;

namespace Scheder.Tools;

public class MDTools
{
    private static readonly Regex MarkdownV2Regex = new Regex(@"([_\*\[\]\(\)~`>#\+\-=\|\{\}\.!\\])", RegexOptions.Compiled);

    public static string EscapeMarkdownV2(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("\\", "\\\\") // Обязательно ПЕРВЫМ!
            .Replace("#", "\\#")
            .Replace(".", "\\.")
            .Replace("_", "\\_")
            .Replace("*", "\\*");
    }
    
    
}