using System.Text.Json;
using Npgsql;
using Scheder.Config;
using Scheder.Services.JournalAPI;

namespace Scheder.Services.Database;

public static class Memory
{
    private static readonly string
        ConnectionString = $"Host={Env.DB_HOST};Port={Env.DB_PORT};Database={Env.DB_NAME};Username={Env.DB_USER};Password={Env.DB_PASS}";

    private static NpgsqlConnection GetConnection() => new(ConnectionString);


    
    public static async Task InitializeAsync()
    {
        Console.WriteLine("Connect string: "+ConnectionString);
        await using var conn = GetConnection();
        await conn.OpenAsync();

        
        const string sql = @"
            CREATE TABLE IF NOT EXISTS users (
                uid             BIGINT PRIMARY KEY,
                auth            JSONB   NOT NULL DEFAULT '{}',
                action          TEXT,
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
                as_teacher      BOOLEAN NOT NULL DEFAULT FALSE,
                reminders       JSONB   NOT NULL DEFAULT '[]',
                settings        JSONB   NOT NULL DEFAULT '[]',
                date_listeners  JSONB   NOT NULL DEFAULT '[]'
            );";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();

        await using var cmd2 = new NpgsqlCommand(sql2, conn);
        await cmd2.ExecuteNonQueryAsync();

        await using var cmd3 = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS settings JSONB", conn);
        await cmd3.ExecuteNonQueryAsync();

        await using var cmd4 = new NpgsqlCommand("ALTER TABLE tggroups ADD COLUMN IF NOT EXISTS settings JSONB", conn);
        await cmd4.ExecuteNonQueryAsync();
    }

    public static class User
    {
        public static async Task<bool> IsUserExistsAsync(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM users WHERE uid = @uid LIMIT 1", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync();
        }

        public static async Task<UserRecord?> GetUserAsync(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT uid, auth, action, as_teacher, linked_groups, reminders, date_listeners, gmt " +
                "FROM users WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new UserRecord
            {
                Uid = reader.GetInt64(0),
                AuthJson = reader.IsDBNull(1) ? "{}" : reader.GetString(1),
                Action = reader.IsDBNull(2) ? null : reader.GetString(2),
                AsTeacher = reader.GetBoolean(3),
                LinkedGroupsJson = reader.IsDBNull(4) ? "[]" : reader.GetString(4),
                RemindersJson = reader.IsDBNull(5) ? "[]" : reader.GetString(5),
                DateListenersJson = reader.IsDBNull(6) ? "[]" : reader.GetString(6),
                GMT = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            };
        }

        public static async Task SetActionAsync(long uid, string? actionName)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET action = @action WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("action", (object?)actionName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("uid", uid);

            await cmd.ExecuteNonQueryAsync();
        }

        // В оригинале getAction(uid, action_name) на самом деле читает
        // произвольное поле документа по его имени. В реляционной схеме
        // такой универсальный доступ по имени колонки делать небезопасно
        // (риск SQL-инъекции через имя колонки), поэтому даём отдельный
        // метод именно для поля action.
        public static async Task<string?> GetActionAsync(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT action FROM users WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
        
        public static async Task<List<long>> GetLinkedGroups(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT linked_groups FROM users WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            var result = await cmd.ExecuteScalarAsync();

            if (result is not string json)
                return [];

            return JsonSerializer.Deserialize<List<long>>(json) ?? [];
        }

        public static async Task RegisterAsync(long uid, string? action = null)
        {
            if (await IsUserExistsAsync(uid))
                return;

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "INSERT INTO users (uid, auth, action, as_teacher, linked_groups, reminders, date_listeners) " +
                "VALUES (@uid, @auth::jsonb, @action, FALSE, @groups::jsonb, @reminders::jsonb, @listeners::jsonb) " +
                "ON CONFLICT (uid) DO NOTHING", conn);

            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("auth", "{}");
            cmd.Parameters.AddWithValue("action", (object?)action ?? DBNull.Value);
            cmd.Parameters.AddWithValue("groups", "[]");
            cmd.Parameters.AddWithValue("reminders", "[]");
            cmd.Parameters.AddWithValue("listeners", "[]");

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task SaveAuthAsync(long uid, string login, string password)
        {
            var authJson = JsonSerializer.Serialize(new
            {
                login,
                password,
                JWTRefreshTime = DateTime.Now
            });

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET auth = @auth::jsonb WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("auth", authJson);
            cmd.Parameters.AddWithValue("uid", uid);

            await cmd.ExecuteNonQueryAsync();
        }
        
        public static async Task SetCity(long uid, string? city)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET city = @city WHERE uid = @uid", conn);

            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("city", (object?)city ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }


        public static async Task<string?> GetCity(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT city FROM users WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
        
        public static async Task LinkGroup(long uid, long groupId)
        {

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET linked_groups = linked_groups || to_jsonb(@groupId) WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("groupId", groupId);

            await cmd.ExecuteNonQueryAsync();
        }
        
        public static async Task UnlinkGroup(long uid, long groupId)
        {

            await using var conn = GetConnection();
            await conn.OpenAsync();



            await using var cmd = new NpgsqlCommand(
                @"UPDATE users SET linked_groups = 
                                ( SELECT COALESCE(jsonb_agg(value), '[]'::jsonb)
                                    FROM jsonb_array_elements(linked_groups) AS t(value)
                                    WHERE value <> to_jsonb(@groupId)
                          ) WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("groupId", groupId);

            await cmd.ExecuteNonQueryAsync();
        }
        
        
        public static async Task<bool> HasAuth(long uid)
        {

            await using var conn = GetConnection();
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (" +
                "  SELECT 1 FROM users " +
                "  WHERE uid = @uid " +
                "    AND (" +
                "      (auth->>'login' IS NOT NULL AND auth->>'login' <> '') " +
                "      OR " +
                "      (auth->>'username' IS NOT NULL AND auth->>'username' <> '')" +
                "    )" +
                ")", conn);
        
            cmd.Parameters.AddWithValue("uid", uid);

            return (bool)(await cmd.ExecuteScalarAsync() ?? false);
        }


        public static async Task<AuthClass?> GetAuthAsync(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT auth FROM users WHERE uid = @uid", conn);

            cmd.Parameters.AddWithValue("uid", uid);

            var result = await cmd.ExecuteScalarAsync();

            if (result is not string json) return null;
            
            var auth = JsonSerializer.Deserialize<AuthClass>(json);
            return auth;

        }

        // Обновляет JWT и время его получения, не трогая остальные поля
        // объекта auth (login/password остаются как были). Используем
        // jsonb-мердж (||), а не полную перезапись auth.
        public static async Task SetJWTAsync(long uid, string newToken)
        {
            var patch = JsonSerializer.Serialize(new
            {
                JWT = newToken,
                JWTRefreshTime = DateTime.Now
            });

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET auth = auth || @patch::jsonb WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("patch", patch);
            cmd.Parameters.AddWithValue("uid", uid);

            await cmd.ExecuteNonQueryAsync();
        }
        

        // Возвращает (jwt, gotAt). Если пользователя нет, или поля не
        // заполнены — соответствующие значения будут null.
        public static async Task<(string? jwt, DateTime? gotAt)> GetJWTAsync(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT auth->>'JWT', auth->>'JWTRefreshTime' FROM users WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return (null, null);

            var jwt = reader.IsDBNull(0) ? null : reader.GetString(0);
            var dateStr = reader.IsDBNull(1) ? null : reader.GetString(1);

            DateTime? gotAt = dateStr != null && DateTime.TryParse(dateStr, out var parsed)
                ? parsed
                : null;

            return (jwt, gotAt);
        }
        
        
        public static async Task SetGMT(long uid, int jmtValue)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET gmt = @newGmt WHERE uid = @uid", conn);
            
            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("newGmt", jmtValue);

            await cmd.ExecuteNonQueryAsync();
        }


        // Настройки бота (Scheder.Services.Settings) хранятся в колонке users.settings (JSONB)
        // как плоский объект вида {"0": 1, "1": 0, ...}, где ключ — SettingDefinition.Id.
        // Колонка изначально создаётся с DEFAULT '[]' (пустой массив) — на случай, если там
        // ещё не JSON-объект, при записи он приводится к '{}' автоматически.

        public static async Task<Dictionary<int, int>> GetSettingsAsync(long uid)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT settings FROM users WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("uid", uid);

            var result = await cmd.ExecuteScalarAsync();
            if (result is not string json)
                return new Dictionary<int, int>();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<int, int>();

            var dict = new Dictionary<int, int>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (int.TryParse(prop.Name, out var id) && prop.Value.ValueKind == JsonValueKind.Number)
                    dict[id] = prop.Value.GetInt32();
            }

            return dict;
        }

        public static async Task<int?> GetSettingAsync(long uid, int settingId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT settings->>@key FROM users WHERE uid = @uid AND jsonb_typeof(settings) = 'object'", conn);
            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("key", settingId.ToString());

            var result = await cmd.ExecuteScalarAsync();
            if (result is null || result == DBNull.Value)
                return null;

            return int.TryParse(result.ToString(), out var value) ? value : null;
        }

        public static async Task SetSettingAsync(long uid, int settingId, int value)
        {
            var patch = JsonSerializer.Serialize(new Dictionary<string, int> { [settingId.ToString()] = value });

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE users SET settings = " +
                "(CASE WHEN jsonb_typeof(settings) = 'object' THEN settings ELSE '{}'::jsonb END) || @patch::jsonb " +
                "WHERE uid = @uid", conn);
            cmd.Parameters.AddWithValue("patch", patch);
            cmd.Parameters.AddWithValue("uid", uid);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    public static class Group
    {
        public static async Task<UserRecord?> getUserObject(long groupId)
        {
            var boundTo = await getGroupBind(groupId);
            if (boundTo == null) return null;
            return await User.GetUserAsync((long) boundTo);
        }
        
        public static async Task<bool> IsGroupExists(long groupId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT EXISTS ( SELECT 1 FROM tggroups WHERE groupId = @groupId )", conn);
            cmd.Parameters.AddWithValue("groupId", groupId);

            return (bool)(await cmd.ExecuteScalarAsync() ?? false);
        }
        
        public static async Task RegisterAsync(long groupId)
        {
            if (await IsGroupExists(groupId))
                return;

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "INSERT INTO tggroups (groupId, bindto, bind2, bindtoken, action, as_teacher, reminders, date_listeners) " +
                "VALUES (@groupId, @bindto, @bind2, @bindtoken, @action, @as_teacher, @reminders::jsonb, @date_listeners::jsonb) " +
                "ON CONFLICT (groupId) DO NOTHING", conn);

            cmd.Parameters.AddWithValue("groupId", groupId);
            cmd.Parameters.Add(new NpgsqlParameter("bindto", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("bind2", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("bindtoken", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("action", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
            cmd.Parameters.AddWithValue("as_teacher", false);
            cmd.Parameters.AddWithValue("reminders", "[]");
            cmd.Parameters.AddWithValue("date_listeners", "[]");

            await cmd.ExecuteNonQueryAsync();
        }
        
        
        public static async Task<bool> IsGroupBind(long groupId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM tggroups WHERE groupId = @groupId AND bindto IS NOT NULL)", conn);
            cmd.Parameters.AddWithValue("groupId", groupId);

            return (bool)(await cmd.ExecuteScalarAsync() ?? false);
        }

        public static async Task setGroupBind(long groupId, long? bindingTo)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "UPDATE tggroups SET bindto = @bindingTo WHERE groupid = @groupId", conn);
            
            cmd.Parameters.AddWithValue("bindingTo", bindingTo == null ? DBNull.Value : bindingTo.Value);
            cmd.Parameters.AddWithValue("groupId", groupId);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<long?> getGroupBind(long groupId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "SELECT bindto FROM tggroups WHERE groupid = @groupId", conn);
            
            cmd.Parameters.AddWithValue("groupId", groupId);

            var result = await cmd.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt64(result);
        }
        
        public static async Task setGroupBindToken(long groupId, string token)
        {
            if (!await IsGroupExists(groupId))
            {
                await RegisterAsync(groupId);
            }

            await using var conn = GetConnection();
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "UPDATE tggroups SET bindToken = @bindToken WHERE groupid = @groupId", conn);
            
            cmd.Parameters.AddWithValue("bindtoken", token);
            cmd.Parameters.AddWithValue("groupId", groupId);

            await cmd.ExecuteNonQueryAsync();
        }
        
        public static async Task<string?> getGroupBindToken(long groupId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "SELECT bindToken FROM tggroups WHERE groupid = @groupId", conn);
            
            cmd.Parameters.AddWithValue("groupId", groupId);

            var result = await cmd.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToString(result);
        }

        
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