namespace Scheder.TelegramInteractions.Commands.Settings.Data;


public static class SettingsList
{
    public static readonly IReadOnlyList<SettingDefinition> All = new List<SettingDefinition>
    {
        new()
        {
            Id = SettingsTypeList.ContextDetection,
            Title = "Контекстная активация",
            Description = "Если в отправленном сообщении есть потребность отправлять список пар (например: \"Какие у нас завтра пары\" — бот без команды пришлет расписание на завтра. Вариант \"Всегда\" рекомендуется только в личных сообщениях",
            Type = SettingType.IntList,
            States = [0, 1, 2],
            StateLabels = ["Отключено", "Начинается со слова \"пары\"", "Всегда"],
            Default = 1,
            GroupDefault = 1
        },
        new()
        {
            Id = SettingsTypeList.AllowWeather,
            Title = "Погода",
            Description = "Показывает погоду если профиль поддерживает. Появляется только в командах /exams и /пары",
            Type = SettingType.IntList,
            States = [0, 1, 2],
            StateLabels = ["Отключена", "Только текстом", "Полная"],
            Default = 2
        },
        new()
        {
            Id = SettingsTypeList.AllowDraft,
            Title = "Показывать Draft",
            Description = "Показывает на какой стадии сейчас бот (В группе показывается как \"Печатает сообщение...\"). Отключите если нужна скорость ответа.",
            Type = SettingType.Bool,
            StateLabels = ["Отключен", "Включить"],
            Default = 1
        },  
        new()
        {
            Id = SettingsTypeList.AllowReactions,
            Title = "Реакция при вызове",
            Description = "Бот поставит реакцию на ваше сообщение когда его попросят показать расписание",
            Type = SettingType.Bool,
            StateLabels = ["Запретить", "Разрешить"],
            Default = 1
        },  
        new()
        {
            Id = SettingsTypeList.AllowDataCaching,
            Title = "Кэш",
            Description = "Кэш сохраняется в среднем в течении 20 минут. Кэшируются: ответы, токен авторизации и список экзаменов. Кэш сбрасывается автоматически или при перезапуске бота",
            Type = SettingType.Bool,
            StateLabels = ["Отключен", "Разрешен"],
            Default = 1
        }
    };

    public static SettingDefinition? GetById(int id) => All.FirstOrDefault(s => s.Id == id);
}