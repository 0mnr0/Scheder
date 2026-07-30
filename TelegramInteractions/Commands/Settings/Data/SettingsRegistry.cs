namespace Scheder.TelegramInteractions.Commands.Settings.Data;


public static class SettingsRegistry
{
    public static readonly IReadOnlyList<SettingDefinition> All = new List<SettingDefinition>
    {
        new()
        {
            Id = SettingsList.ContextDetection,
            Title = "Контекстная активиация",
            Description = "Если в отправленном сообщении есть потребность отправлять список пар (например: \"Какие у нас завтра пары\" — бот без команды пришлет расписание на завтра. Вариант \"Всегда\" рекомендуется только в личных сообщениях",
            Type = SettingType.IntList,
            States = [0, 1, 2],
            StateLabels = ["Отключено", "Начинается со слова \"пары\"", "Всегда"],
            Default = 0
        }
    };

    public static SettingDefinition? GetById(int id) => All.FirstOrDefault(s => s.Id == id);
}