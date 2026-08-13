using Scheder.Services.Database;

namespace Scheder.TelegramInteractions.Commands.Settings.Data;

public class PgSettings : ISettings
{
    public static Task<int?> GetValueAsync(long userId, int settingId, bool isGroup, CancellationToken cancellationToken) =>
        isGroup ? Memory.Group.GetSettingAsync(userId, settingId) : Memory.User.GetSettingAsync(userId, settingId);
 
    public static Task SetValueAsync(long userId, int settingId, int value, bool isGroup, CancellationToken cancellationToken) =>
        isGroup ? Memory.Group.SetSettingAsync(userId, settingId, value) : Memory.User.SetSettingAsync(userId, settingId, value);
 
    public static Task<Dictionary<int, int>> GetAllValuesAsync(long userId, bool isGroup, CancellationToken cancellationToken) =>
        isGroup ? Memory.Group.GetSettingsAsync(userId) : Memory.User.GetSettingsAsync(userId);
    
    
}