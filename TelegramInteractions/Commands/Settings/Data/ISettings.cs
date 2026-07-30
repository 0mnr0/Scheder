namespace Scheder.TelegramInteractions.Commands.Settings.Data;

public interface ISettings
{
    Task<int?> GetValueAsync(long userId, int settingId, CancellationToken cancellationToken);

    Task SetValueAsync(long userId, int settingId, int value, CancellationToken cancellationToken);
    
    Task<Dictionary<int, int>> GetAllValuesAsync(long userId, CancellationToken cancellationToken);
}