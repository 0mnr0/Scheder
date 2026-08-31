namespace Scheder.Services.Database;

using System.Text.Json.Serialization;
using Npgsql;
using NpgsqlTypes;

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


    public static Task InitializeAsync()
    {
        const string sql = """

                                       CREATE TABLE IF NOT EXISTS stats (
                                           id          SERIAL PRIMARY KEY,
                                           action_type TEXT    NOT NULL,
                                           chat_id     BIGINT  NOT NULL,
                                           who_asked   BIGINT  NOT NULL,
                                           time        TEXT    NOT NULL,
                                           data        JSONB   NOT NULL DEFAULT '{}'
                                       );
                           """;

        return ExecuteNonQuery(sql);
    }
    
    public static Task SaveAsync(string actionType, long chatId, long whoAsked, string time, string json) =>
        ExecuteNonQuery(
            "INSERT INTO stats (action_type, chat_id, who_asked, time, data) " +
            "VALUES (@actionType, @chatId, @whoAsked, @time, @data::jsonb)",
            Param("actionType", actionType, NpgsqlDbType.Text),
            Param("chatId", chatId, NpgsqlDbType.Bigint),
            Param("whoAsked", whoAsked, NpgsqlDbType.Bigint),
            Param("time", time, NpgsqlDbType.Text),
            Param("data", json, NpgsqlDbType.Jsonb));
}



public class StatsData
{
    [JsonPropertyName("Total")]
    public int Total { get; set; }

    [JsonPropertyName("Context")]
    public int Context { get; set; }

    [JsonPropertyName("Token")]
    public int Token { get; set; }

    [JsonPropertyName("ParseJournal")]
    public int ParseJournal { get; set; }

    [JsonPropertyName("Build")]
    public int Build { get; set; }

    [JsonPropertyName("Delivery")]
    public int Delivery { get; set; }

    [JsonPropertyName("WeatherParse")]
    public int WeatherParse { get; set; }

    [JsonPropertyName("WeatherRender")]
    public int WeatherRender { get; set; }

    [JsonPropertyName("DraftTime")]
    public int? DraftTime { get; set; }

    [JsonPropertyName("TriggerPercent")]
    public string? TriggerPercent { get; set; }
}