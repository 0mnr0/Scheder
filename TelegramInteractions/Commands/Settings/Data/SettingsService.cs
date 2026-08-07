namespace Scheder.TelegramInteractions.Commands.Settings.Data;


public class SettingsService
{
    private static readonly PgSettings Repo = new();
    
    
    public static async Task<Dictionary<int, int>> GetEffectiveValuesAsync(long userId, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetAllValuesAsync(userId, cancellationToken);
        var result = new Dictionary<int, int>();

        foreach (var def in SettingsRegistry.All)
        {
            result[def.Id] = stored.TryGetValue(def.Id, out var v) ? v : def.Default;
        }

        return result;
    }

    public static async Task<int> GetEffectiveValueAsync(long userId, SettingDefinition def, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetValueAsync(userId, def.Id, cancellationToken);
        return stored ?? def.Default;
    }

    public static async Task<int?> GetValue(long userId, int settingId, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetValueAsync(userId, settingId, cancellationToken);
        var defValue = SettingsRegistry.All[settingId].Default;
        if (stored == null) return defValue;
        
        return stored;
    }

    public static async Task<bool> GetBool(long userId, int settingId, CancellationToken cancellationToken)
    {
        var stored = await Repo.GetValueAsync(userId, settingId, cancellationToken);
        var defValue = SettingsRegistry.All[settingId].Default;
        if (stored == null) return defValue is 1;
        
        return stored is 1;
    }


    public static async Task<int> ToggleAsync(long userId, SettingDefinition def, CancellationToken cancellationToken)
    {
        var current = await GetEffectiveValueAsync(userId, def, cancellationToken);
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

        await Repo.SetValueAsync(userId, def.Id, next, cancellationToken);
        return next;
    }
}