using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using NpgsqlTypes;
using Scheder.Services.JournalAPI;

namespace Scheder.Services.Database;

public static class Memory
{
    private static readonly string
        ConnectionString = DatabaseTools.ConnectionString;

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }
    
    private static NpgsqlParameter Param(string name, object? value) =>
        new() { ParameterName = name, Value = value ?? DBNull.Value };
 
    private static NpgsqlParameter Param(string name, object? value, NpgsqlDbType type) =>
        new(name, type) { Value = value ?? DBNull.Value };

    private static async Task<object?> ExecuteScalar(string sql, params NpgsqlParameter[] parameters)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task<int> ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        return await cmd.ExecuteNonQueryAsync();
    }
    
    private static async Task<T?> ExecuteReader<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        params NpgsqlParameter[] parameters)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : default;
    }

    private static async Task<List<T>> ExecuteReaderList<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        params NpgsqlParameter[] parameters)
    {
        var list = new List<T>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(map(reader));
        return list;
    }


    public static async Task InitializeAsync()
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS users (
                uid             BIGINT PRIMARY KEY,
                auth            JSONB   NOT NULL DEFAULT '{}',
                action          TEXT,
                city            TEXT,
                gmt             INT,
                as_teacher      BOOLEAN NOT NULL DEFAULT FALSE,
                linked_groups   JSONB   NOT NULL DEFAULT '[]',
                reminders       JSONB   NOT NULL DEFAULT '[]',
                settings        JSONB   NOT NULL DEFAULT '[]',
                date_listeners  JSONB   NOT NULL DEFAULT '[]'
            );";

        const string sql2 = @"
            CREATE TABLE IF NOT EXISTS tggroups (
                groupId         BIGINT PRIMARY KEY,
                bindto          BIGINT,
                bind2           BIGINT,
                bindToken       TEXT,
                action          TEXT,
                gmt             INT,
                CITY            TEXT,
                as_teacher      BOOLEAN NOT NULL DEFAULT FALSE,
                reminders       JSONB   NOT NULL DEFAULT '[]',
                settings        JSONB   NOT NULL DEFAULT '[]',
                date_listeners  JSONB   NOT NULL DEFAULT '[]'
            );";

        await ExecuteNonQuery(sql);
        await ExecuteNonQuery(sql2);
    }
    
    

    private static async Task<Dictionary<int, int>> GetSettingsCoreAsync(string table, string idColumn, long id)
    {
        var json = (string?)await ExecuteScalar(
            $"SELECT settings FROM {table} WHERE {idColumn} = @id", Param("id", id));

        var dict = new Dictionary<int, int>();
        if (json is null)
            return dict;

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return dict;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (int.TryParse(prop.Name, out var settingId) && prop.Value.ValueKind == JsonValueKind.Number)
                dict[settingId] = prop.Value.GetInt32();
        }

        return dict;
    }

    private static async Task<int?> GetSettingCoreAsync(string table, string idColumn, long id, int settingId)
    {
        var result = await ExecuteScalar(
            $"SELECT settings->>@key FROM {table} WHERE {idColumn} = @id AND jsonb_typeof(settings) = 'object'",
            Param("id", id), Param("key", settingId.ToString()));

        if (result is null || result == DBNull.Value)
            return null;

        return int.TryParse(result.ToString(), out var value) ? value : null;
    }

    private static async Task SetSettingCoreAsync(string table, string idColumn, long id, int settingId, int value)
    {
        var patch = JsonSerializer.Serialize(new Dictionary<string, int> { [settingId.ToString()] = value });

        await ExecuteNonQuery(
            $"UPDATE {table} SET settings = " +
            "(CASE WHEN jsonb_typeof(settings) = 'object' THEN settings ELSE '{}'::jsonb END) || @patch::jsonb " +
            $"WHERE {idColumn} = @id",
            Param("patch", patch), Param("id", id));
    }


    private static async Task<DayListener> AddDayListenerCoreAsync(string table, string idColumn, long id, string date, int? threadId = null)
    {
        var listener = new DayListener { Date = date, Hash = Guid.NewGuid().ToString("N"), ThreadId = threadId };
        var json = JsonSerializer.Serialize(listener);

        await ExecuteNonQuery(
            $"UPDATE {table} SET date_listeners = date_listeners || @item::jsonb WHERE {idColumn} = @id",
            Param("item", json), Param("id", id));

        return listener;
    }

    private static async Task<List<DayListener>> GetDayListenersCoreAsync(string table, string idColumn, long id)
    {
        var json = (string?)await ExecuteScalar(
            $"SELECT date_listeners FROM {table} WHERE {idColumn} = @id", Param("id", id));

        if (json is null)
            return [];

        return JsonSerializer.Deserialize<List<DayListener>>(json) ?? [];
    }

    private static Task RemoveDayListenerByDateCoreAsync(string table, string idColumn, long id, string date) =>
        ExecuteNonQuery(
            $"""
             UPDATE {table} SET date_listeners =
                 ( SELECT COALESCE(jsonb_agg(value), '[]'::jsonb)
                     FROM jsonb_array_elements(date_listeners) AS t(value)
                     WHERE value->>'date' <> @date
                 ) WHERE {idColumn} = @id
             """,
            Param("id", id), Param("date", date));

    private static Task RemoveDayListenerByHashCoreAsync(string table, string idColumn, long id, string hash) =>
        ExecuteNonQuery(
            $"""
             UPDATE {table} SET date_listeners =
                 ( SELECT COALESCE(jsonb_agg(value), '[]'::jsonb)
                     FROM jsonb_array_elements(date_listeners) AS t(value)
                     WHERE value->>'hash' <> @hash
                 ) WHERE {idColumn} = @id
             """,
            Param("id", id), Param("hash", hash));

    private static Task ClearDayListenersCoreAsync(string table, string idColumn, long id) =>
        ExecuteNonQuery(
            $"UPDATE {table} SET date_listeners = '[]'::jsonb WHERE {idColumn} = @id",
            Param("id", id));

    private static Task UpdateDayListenerCoreAsync(string table, string idColumn, long id, string date, string newHash) =>
        ExecuteNonQuery(
            $"""
             UPDATE {table} SET date_listeners =
                 ( SELECT COALESCE(jsonb_agg(
                             CASE WHEN value->>'date' = @date
                                  THEN jsonb_set(value, ARRAY['hash'], to_jsonb(@newHash::text))
                                  ELSE value END
                          ), '[]'::jsonb)
                     FROM jsonb_array_elements(date_listeners) AS t(value)
                 ) WHERE {idColumn} = @id
             """,
            Param("id", id), Param("date", date), Param("newHash", newHash));

    /// <summary>
    /// Возвращает список всех юзеров и групп, у которых date_listeners.Length > 0.
    /// </summary>
    public static async Task<List<(long Id, bool IsGroup)>> GetAllWithDayListeners()
    {
        var result = new List<(long Id, bool IsGroup)>();

        var users = await ExecuteReaderList(
            "SELECT uid FROM users WHERE jsonb_array_length(date_listeners) > 0",
            reader => reader.GetInt64(0));
        result.AddRange(users.Select(uid => (uid, false)));

        var groups = await ExecuteReaderList(
            "SELECT groupid FROM tggroups WHERE jsonb_array_length(date_listeners) > 0",
            reader => reader.GetInt64(0));
        result.AddRange(groups.Select(groupId => (groupId, true)));

        return result;
    }


    public static class User
    {
        public static async Task<bool> IsUserExistsAsync(long uid) =>
            await ExecuteScalar(
                "SELECT 1 FROM users WHERE uid = @uid LIMIT 1", Param("uid", uid)) != null;

        public static Task<UserRecord?> GetUserAsync(long uid) =>
            ExecuteReader(
                "SELECT uid, auth, action, as_teacher, linked_groups, reminders, date_listeners, gmt " +
                "FROM users WHERE uid = @uid",
                reader => new UserRecord
                {
                    Uid = reader.GetInt64(0),
                    AuthJson = reader.IsDBNull(1) ? "{}" : reader.GetString(1),
                    Action = reader.IsDBNull(2) ? null : reader.GetString(2),
                    AsTeacher = reader.GetBoolean(3),
                    LinkedGroupsJson = reader.IsDBNull(4) ? "[]" : reader.GetString(4),
                    RemindersJson = reader.IsDBNull(5) ? "[]" : reader.GetString(5),
                    DateListenersJson = reader.IsDBNull(6) ? "[]" : reader.GetString(6),
                    GMT = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                },
                Param("uid", uid));

        public static Task SetActionAsync(long uid, string? actionName) =>
            ExecuteNonQuery(
                "UPDATE users SET action = @action WHERE uid = @uid",
                Param("action", actionName), Param("uid", uid));


        
        public static async Task<string?> GetActionAsync(long uid) =>
            (string?)await ExecuteScalar(
                "SELECT action FROM users WHERE uid = @uid", Param("uid", uid));

        public static async Task<List<long>> GetLinkedGroups(long uid)
        {
            var json = (string?)await ExecuteScalar(
                "SELECT linked_groups FROM users WHERE uid = @uid", Param("uid", uid));

            if (json is null)
                return [];

            return JsonSerializer.Deserialize<List<long>>(json) ?? [];
        }

        public static async Task RegisterAsync(long uid, string? action = null)
        {
            if (await IsUserExistsAsync(uid))
                return;

            await ExecuteNonQuery(
                "INSERT INTO users (uid, auth, action, as_teacher, linked_groups, reminders, date_listeners) " +
                "VALUES (@uid, @auth::jsonb, @action, FALSE, @groups::jsonb, @reminders::jsonb, @listeners::jsonb) " +
                "ON CONFLICT (uid) DO NOTHING",
                Param("uid", uid),
                Param("auth", "{}"),
                Param("action", action),
                Param("groups", "[]"),
                Param("reminders", "[]"),
                Param("listeners", "[]"));
        }

        public static Task SaveAuthAsync(long uid, string login, string password)
        {
            var authJson = JsonSerializer.Serialize(new
            {
                login,
                password,
                JWTRefreshTime = DateTime.Now
            });

            return ExecuteNonQuery(
                "UPDATE users SET auth = @auth::jsonb WHERE uid = @uid",
                Param("auth", authJson), Param("uid", uid));
        }

        public static Task SetCity(long uid, string? city) =>
            ExecuteNonQuery(
                "UPDATE users SET city = @city WHERE uid = @uid",
                Param("uid", uid), Param("city", city));

        public static async Task<string?> GetCity(long uid) =>
            (string?)await ExecuteScalar(
                "SELECT city FROM users WHERE uid = @uid", Param("uid", uid));

        public static Task LinkGroup(long uid, long groupId) =>
            ExecuteNonQuery(
                "UPDATE users SET linked_groups = linked_groups || to_jsonb(@groupId) WHERE uid = @uid",
                Param("uid", uid), Param("groupId", groupId));

        public static Task UnlinkGroup(long uid, long groupId) =>
            ExecuteNonQuery(
                """
                UPDATE users SET linked_groups = 
                                                ( SELECT COALESCE(jsonb_agg(value), '[]'::jsonb)
                                                    FROM jsonb_array_elements(linked_groups) AS t(value)
                                                    WHERE value <> to_jsonb(@groupId)
                                          ) WHERE uid = @uid
                """,
                Param("uid", uid), Param("groupId", groupId));

        public static async Task<bool> HasAuth(long uid) =>
            (bool)(await ExecuteScalar(
                "SELECT EXISTS (" +
                "  SELECT 1 FROM users " +
                "  WHERE uid = @uid " +
                "    AND (" +
                "      (auth->>'login' IS NOT NULL AND auth->>'login' <> '') " +
                "      OR " +
                "      (auth->>'username' IS NOT NULL AND auth->>'username' <> '')" +
                "    )" +
                ")",
                Param("uid", uid)) ?? false);

        public static async Task<AuthClass?> GetAuthAsync(long uid)
        {
            var json = (string?)await ExecuteScalar(
                "SELECT auth FROM users WHERE uid = @uid", Param("uid", uid));

            return json is null ? null : JsonSerializer.Deserialize<AuthClass>(json);
        }
        
        public static Task SetJwtAsync(long uid, string newToken)
        {
            var patch = JsonSerializer.Serialize(new
            {
                JWT = newToken,
                JWTRefreshTime = DateTime.Now
            });

            return ExecuteNonQuery(
                "UPDATE users SET auth = auth || @patch::jsonb WHERE uid = @uid",
                Param("patch", patch), Param("uid", uid));
        }


        public static Task<(string? jwt, DateTime? gotAt)> GetJwtAsync(long uid) =>
            ExecuteReader(
                "SELECT auth->>'JWT', auth->>'JWTRefreshTime' FROM users WHERE uid = @uid",
                reader =>
                {
                    var jwt = reader.IsDBNull(0) ? null : reader.GetString(0);
                    var dateStr = reader.IsDBNull(1) ? null : reader.GetString(1);

                    DateTime? gotAt = dateStr != null && DateTime.TryParse(dateStr, out var parsed)
                        ? parsed
                        : null;

                    return (jwt, gotAt);
                },
                Param("uid", uid));

        public static Task SetGmt(long uid, int jmtValue) =>
            ExecuteNonQuery(
                "UPDATE users SET gmt = @newGmt WHERE uid = @uid",
                Param("uid", uid), Param("newGmt", jmtValue));

        public static Task<Dictionary<int, int>> GetSettingsAsync(long uid) =>
            GetSettingsCoreAsync("users", "uid", uid);

        public static Task<int?> GetSettingAsync(long uid, int settingId) =>
            GetSettingCoreAsync("users", "uid", uid, settingId);

        public static Task SetSettingAsync(long uid, int settingId, int value) =>
            SetSettingCoreAsync("users", "uid", uid, settingId, value);

        /// <summary>Добавляет day-listener на дату <paramref name="date"/> и возвращает созданный объект (с хешем).</summary>
        public static Task<DayListener> AddDayListener(long uid, string date, int? threadId = null) =>
            AddDayListenerCoreAsync("users", "uid", uid, date, threadId);

        public static Task<List<DayListener>> GetDayListeners(long uid) =>
            GetDayListenersCoreAsync("users", "uid", uid);

        /// <summary>Удаляет один day-listener с указанной датой.</summary>
        public static Task RemoveDayListener(long uid, string date) =>
            RemoveDayListenerByDateCoreAsync("users", "uid", uid, date);

        /// <summary>Удаляет конкретный day-listener (по его хешу).</summary>
        public static Task RemoveDayListener(long uid, DayListener listener) =>
            RemoveDayListenerByHashCoreAsync("users", "uid", uid, listener.Hash);

        public static Task ClearDayListeners(long uid) =>
            ClearDayListenersCoreAsync("users", "uid", uid);

        /// <summary>Обновляет хеш day-listener'а с указанной датой на <paramref name="newHash"/>.</summary>
        public static Task UpdateDayListener(long uid, string date, string newHash) =>
            UpdateDayListenerCoreAsync("users", "uid", uid, date, newHash);
        
        public static Task UpdateDayListener(long uid, DayListener listener) =>
            UpdateDayListenerCoreAsync("users", "uid", uid,
                listener.Date, listener.Hash);
    }

    public static class Group
    {
        public static async Task<UserRecord?> GetUserObject(long groupId)
        {
            var boundTo = await GetGroupBind(groupId);
            if (boundTo == null) return null;
            return await User.GetUserAsync((long)boundTo);
        }

        public static async Task<bool> IsGroupExists(long groupId) =>
            (bool) (await ExecuteScalar(
                "SELECT EXISTS ( SELECT 1 FROM tggroups WHERE groupId = @groupId )",
                Param("groupId", groupId)) ?? false);

        public static async Task RegisterAsync(long groupId)
        {
            if (await IsGroupExists(groupId))
                return;
            
            await ExecuteNonQuery(
                "INSERT INTO tggroups (groupId, bindto, bind2, bindtoken, action, as_teacher, reminders, date_listeners) " +
                "VALUES (@groupId, @bindto, @bind2, @bindtoken, @action, @as_teacher, @reminders::jsonb, @date_listeners::jsonb) " +
                "ON CONFLICT (groupId) DO NOTHING",
                Param("groupId", groupId),
                Param("bindto", null, NpgsqlDbType.Bigint),
                Param("bind2", null, NpgsqlDbType.Bigint),
                Param("bindtoken", null, NpgsqlDbType.Text),
                Param("action", null, NpgsqlDbType.Text),
                Param("as_teacher", false),
                Param("reminders", "[]"),
                Param("date_listeners", "[]"));
        }

        public static async Task<bool> IsGroupBind(long groupId) =>
            (bool)(await ExecuteScalar(
                "SELECT EXISTS (SELECT 1 FROM tggroups WHERE groupId = @groupId AND bindto IS NOT NULL)",
                Param("groupId", groupId)) ?? false);

        public static Task SetGroupBind(long groupId, long? bindingTo) {
            return ExecuteNonQuery(
                "UPDATE tggroups SET bindto = @bindingTo WHERE groupid = @groupId",
                Param("bindingTo", bindingTo), Param("groupId", groupId));
        }

        public static async Task<long?> GetGroupBind(long groupId)
        {
            var result = await ExecuteScalar(
                "SELECT bindto FROM tggroups WHERE groupid = @groupId", Param("groupId", groupId));

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt64(result);
        }

        public static async Task SetGroupBindToken(long groupId, string token)
        {
            if (!await IsGroupExists(groupId))
                await RegisterAsync(groupId);

            await ExecuteNonQuery(
                "UPDATE tggroups SET bindToken = @bindToken WHERE groupid = @groupId",
                Param("bindtoken", token), Param("groupId", groupId));
        }

        public static async Task<string?> GetGroupBindToken(long groupId)
        {
            var result = await ExecuteScalar(
                "SELECT bindToken FROM tggroups WHERE groupid = @groupId", Param("groupId", groupId));

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToString(result);
        }

        public static Task<Dictionary<int, int>> GetSettingsAsync(long groupId) =>
            GetSettingsCoreAsync("tggroups", "groupid", groupId);

        public static Task<int?> GetSettingAsync(long groupId, int settingId) =>
            GetSettingCoreAsync("tggroups", "groupid", groupId, settingId);

        public static Task SetSettingAsync(long groupId, int settingId, int value) =>
            SetSettingCoreAsync("tggroups", "groupid", groupId, settingId, value);
        
        public static Task<DayListener> AddDayListener(long groupId, string date, int? threadId) =>
            AddDayListenerCoreAsync("tggroups", "groupid", groupId, date, threadId);

        public static Task<List<DayListener>> GetDayListeners(long groupId) =>
            GetDayListenersCoreAsync("tggroups", "groupid", groupId);
        
        public static Task RemoveDayListener(long groupId, string date) =>
            RemoveDayListenerByDateCoreAsync("tggroups", "groupid", groupId, date);
        public static Task RemoveDayListener(long groupId, DayListener listener) =>
            RemoveDayListenerByHashCoreAsync("tggroups", "groupid", groupId, listener.Hash);
        public static Task ClearDayListeners(long groupId) =>
            ClearDayListenersCoreAsync("tggroups", "groupid", groupId);
        public static Task UpdateDayListener(long groupId, string date, string newHash) =>
            UpdateDayListenerCoreAsync("tggroups", "groupid", groupId, date, newHash);
        public static Task UpdateDayListener(long groupId, DayListener listener) =>
            UpdateDayListenerCoreAsync("tggroups", "groupid", groupId, listener.Date, listener.Hash);
    }

    public class DayListener
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = "";
        
        [JsonPropertyName("threadId")]
        public int? ThreadId { get; set; } = 0;
    }

    public class UserRecord
    {
        public long Uid { get; set; }
        public string AuthJson { get; set; } = "{}";
        public string? Action { get; set; }
        public bool AsTeacher { get; set; }
        public string LinkedGroupsJson { get; set; } = "[]";
        public string RemindersJson { get; set; } = "[]";
        public string DateListenersJson { get; set; } = "[]";
        public int GMT { get; set; } = 0;
    }
}