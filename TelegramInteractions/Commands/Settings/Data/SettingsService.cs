namespace Scheder.TelegramInteractions.Commands.Settings.Data;


public class SettingsService
{
    private static readonly PgSettings Repo = new();
    
    
    public static async Task<Dictionary<int, int>> GetEffectiveValuesAsync(long userId, bool isGroup, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetAllValuesAsync(userId, isGroup, cancellationToken);
        var result = new Dictionary<int, int>();

        foreach (var def in SettingsRegistry.All)
        {
            var defValue = def.Default;
            if (isGroup && def.GroupDefault is not null) {defValue = (int) def.GroupDefault;}
            
            result[def.Id] = stored.GetValueOrDefault(def.Id, defValue);
        }

        return result;
    }

    public static async Task<int> GetEffectiveValueAsync(long userId, SettingDefinition def, bool isGroup, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetValueAsync(userId, def.Id, isGroup, cancellationToken);
        var setting = SettingsRegistry.All[def.Id];
        var defValue = setting.Default;
        if (isGroup && setting.GroupDefault is not null) {defValue = (int) setting.GroupDefault;}
        
        return stored ?? defValue;
    }

    public static async Task<int?> GetValue(long userId, int settingId, bool isGroup, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetValueAsync(userId, settingId, isGroup, cancellationToken);
        var setting = SettingsRegistry.All[settingId];
        var defValue = setting.Default;
        if (isGroup && setting.GroupDefault is not null) {defValue = (int) setting.GroupDefault;}
        
        if (stored == null) return defValue;
        
        return stored;
    }

    public static async Task<bool> GetBool(long userId, int settingId, bool isGroup, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetValueAsync(userId, settingId, isGroup, cancellationToken);
        var setting = SettingsRegistry.All[settingId];
        var defValue = setting.Default;
        if (isGroup && setting.GroupDefault is not null) {defValue = (int) setting.GroupDefault;}
        
        if (stored == null) return defValue is 1;
        
        return stored is 1;
    }


    public static async Task<int> ToggleAsync(long userId, SettingDefinition def, bool isGroup, CancellationToken cancellationToken)
    {
        var current = await GetEffectiveValueAsync(userId, def, isGroup, cancellationToken);
        int next;

        if (def.Type == SettingType.Bool)
        {
            next = current == 0 ? 1 : 0;
        }
        else
        {
            var states = def.States
                ?? throw new InvalidOperationException($"Настройка {def.Id} имеет Type=IntList, но States не задан.");

            var idx = Array.IndexOf(states, current);
            next = states[(idx + 1) % states.Length];
        }

        await Repo.SetValueAsync(userId, def.Id, next, isGroup, cancellationToken);
        return next;
    }
}