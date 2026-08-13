namespace Scheder.TelegramInteractions.Commands.Settings.Data;

public interface ISettings
{
    Task<int?> GetValueAsync(long userId, int settingId, bool isGroup, CancellationToken cancellationToken);

    Task SetValueAsync(long userId, int settingId, int value, bool isGroup, CancellationToken cancellationToken);
    
    Task<Dictionary<int, int>> GetAllValuesAsync(long userId, bool isGroup, CancellationToken cancellationToken);
}