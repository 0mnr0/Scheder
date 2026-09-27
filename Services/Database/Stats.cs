namespace Scheder.Services.Database;

using Npgsql;
using NpgsqlTypes;

public record StatRecord(int Id, string Key, string Value, DateTime When);

public static class Stats
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

    private static async Task<int> ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<StatRecord>> ExecuteQuery(string sql, params NpgsqlParameter[] parameters)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        await using var reader = await cmd.ExecuteReaderAsync();

        var result = new List<StatRecord>();
        while (await reader.ReadAsync())
        {
            result.Add(new StatRecord(
                reader.GetInt32(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("key")),
                reader.GetString(reader.GetOrdinal("value")),
                reader.GetDateTime(reader.GetOrdinal("time"))
            ));
        }

        return result;
    }

    public static Task InitializeAsync()
    {
        const string sql = """

                                       CREATE TABLE IF NOT EXISTS stats (
                                           id    SERIAL PRIMARY KEY,
                                           key   TEXT        NOT NULL,
                                           value TEXT        NOT NULL,
                                           time  TIMESTAMPTZ NOT NULL
                                       );
                           """;

        return ExecuteNonQuery(sql);
    }

    // Вставляет запись (key, value, when) в БД
    public static Task InsertStatAsync(string key, string value, DateTime when) =>
        ExecuteNonQuery(
            "INSERT INTO stats (key, value, time) VALUES (@key, @value, @time)",
            Param("key", key, NpgsqlDbType.Text),
            Param("value", value, NpgsqlDbType.Text),
            Param("time", when, NpgsqlDbType.TimestampTz));

    // Возвращает все записи по указанному ключу
    public static Task<List<StatRecord>> GetStatAsync(string key) =>
        ExecuteQuery(
            "SELECT id, key, value, time FROM stats WHERE key = @key ORDER BY time",
            Param("key", key, NpgsqlDbType.Text));

    // Возвращает все записи
    public static Task<List<StatRecord>> GetStatsAsync() =>
        ExecuteQuery("SELECT id, key, value, time FROM stats ORDER BY time");
}