namespace Scheder.TelegramInteractions.Commands.Settings.Data;

using Telegram.Bot.Types.ReplyMarkups;



public static class SettingsUi
{
    private const int PageSize = 10;

    public static (string Text, InlineKeyboardMarkup Keyboard) BuildListView(
        int page,
        IReadOnlyDictionary<int, int> values)
    {
        var all = SettingsList.All;
        var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var rows = new List<InlineKeyboardButton[]>();

        foreach (var def in all.Skip(page * PageSize).Take(PageSize))
        {
            var value = values.TryGetValue(def.Id, out var v) ? v : def.Default;
            var label = FormatButtonLabel(def, value);
            
            var callback = def.Description is not null
                ? $"setting:o:{def.Id}:{page}"
                : $"setting:t:{def.Id}:l:{page}";

            rows.Add([InlineKeyboardButton.WithCallbackData(label, callback)]);
        }

        if (totalPages > 1)
        {
            var nav = new List<InlineKeyboardButton>
            {
                page > 0
                    ? InlineKeyboardButton.WithCallbackData("◀️", $"setting:l:{page - 1}")
                    : InlineKeyboardButton.WithCallbackData(" ", "setting:noop"),
                InlineKeyboardButton.WithCallbackData($"{page + 1}/{totalPages}", "setting:noop"),
                page < totalPages - 1
                    ? InlineKeyboardButton.WithCallbackData("▶️", $"setting:l:{page + 1}")
                    : InlineKeyboardButton.WithCallbackData(" ", "setting:noop")
            };
            rows.Add([.. nav]);
        }

        return (
            "⚙️ Параметры бота\n\n" +
            "⚠️ Внимание\n" + 
            "Разработчик может принудительно включать или отключать некоторые опции для всех аккаунтов. Ваши настройки не будут затронуты но поведение может отличаться",
            new InlineKeyboardMarkup(rows)
        );
    }

    public static (string Text, InlineKeyboardMarkup Keyboard) BuildDescriptionView(
        SettingDefinition def,
        int value,
        int page)
    {
        var valueText = FormatValueLabel(def, value);
        var text = $"{def.Title}\n\n{def.Description}\n\nТекущее значение: {valueText}";

        var keyboard = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("🔄 Переключить", $"setting:t:{def.Id}:d:{page}")],
            [InlineKeyboardButton.WithCallbackData("◀️ Назад", $"setting:l:{page}")]
        ]);

        return (text, keyboard);
    }

    private static string FormatButtonLabel(SettingDefinition def, int value)
    {
        if (def.Type != SettingType.Bool) return $"{def.Title}: {FormatValueLabel(def, value)}";
        
        var mark = value != 0 ? "✅" : "❌";
        return $"{mark} {def.Title}";

    }

    private static string FormatValueLabel(SettingDefinition def, int value)
    {
        if (def.Type == SettingType.Bool)
            return value != 0 ? "Вкл" : "Выкл";

        if (def.States is not null && def.StateLabels is not null)
        {
            var idx = Array.IndexOf(def.States, value);
            if (idx >= 0 && idx < def.StateLabels.Length)
                return def.StateLabels[idx];
        }

        return value.ToString();
    }
}