namespace Scheder.TelegramInteractions.Commands.Settings.Data;

public interface ISettings
{
    static abstract Task<int?> GetValueAsync(long userId, int settingId, bool isGroup, CancellationToken cancellationToken);

    static abstract Task SetValueAsync(long userId, int settingId, int value, bool isGroup, CancellationToken cancellationToken);
    
    static abstract Task<Dictionary<int, int>> GetAllValuesAsync(long userId, bool isGroup, CancellationToken cancellationToken);
}