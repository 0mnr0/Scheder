using System.Text.Json;
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
            "(CASE WHEN jsonb_typeof(settings) = 'object' THEN settings ELSE '{{}}'::jsonb END) || @patch::jsonb " +
            $"WHERE {idColumn} = @id",
            Param("patch", patch), Param("id", id));
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