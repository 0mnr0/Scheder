namespace Scheder.TelegramInteractions.Commands.Settings.Data;

public enum SettingType
{
    Bool,
    IntList
}


public class SettingDefinition
{
    /// <summary>Короткий числовой ID, используется в callback_data (setting:...:{Id}:...).</summary>
    public required int Id { get; init; }

    /// <summary>Заголовок, отображается на кнопке и в шапке описания.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Если задано — при нажатии на настройку открывается отдельный экран
    /// с текстом описания и кнопкой "Переключить".
    /// Если null — переключение происходит сразу в списке настроек, без промежуточного экрана.
    /// </summary>
    public string? Description { get; init; }

    public required SettingType Type { get; init; }

    /// <summary>
    /// Значение по умолчанию (пока пользователь ничего не сохранил).
    /// Для Bool: 0 или 1. Для IntList: одно из значений States.
    /// </summary>
    public required int Default { get; init; }

    public int? GroupDefault { get; init; } = null;

    /// <summary>
    /// Только для Type == IntList: допустимые значения и порядок переключения по кругу.
    /// </summary>
    public int[]? States { get; init; }

    /// <summary>
    /// Необязательные подписи для значений States (по тому же индексу).
    /// Если не заданы — отображается само число.
    /// </summary>
    public string[]? StateLabels { get; init; }
}