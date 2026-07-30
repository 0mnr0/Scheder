using Scheder.Services.Database;

namespace Scheder.TelegramInteractions.Commands.Settings.Data;

public class PgSettings : ISettings
{
    public Task<int?> GetValueAsync(long userId, int settingId, CancellationToken cancellationToken) =>
        Memory.User.GetSettingAsync(userId, settingId);
 
    public Task SetValueAsync(long userId, int settingId, int value, CancellationToken cancellationToken) =>
        Memory.User.SetSettingAsync(userId, settingId, value);
 
    public Task<Dictionary<int, int>> GetAllValuesAsync(long userId, CancellationToken cancellationToken) =>
        Memory.User.GetSettingsAsync(userId);
}