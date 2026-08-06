using Npgsql;
using Scheder.Tools.Config;

namespace Scheder.Services.Database;

public class DatabaseTools
{
    public const string DatabaseName = "AcademySched";
    
    public static readonly string ConnectionString = 
        $"Host={Env.DB_HOST};Port={Env.DB_PORT};Database={Env.DB_NAME};Username={Env.DB_USER};Password={Env.DB_PASS}";

    
    public static async Task<bool> DatabaseExists()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = Env.DB_HOST,
            Port = Convert.ToInt32(Env.DB_PORT),
            Username = Env.DB_USER,
            Password = Env.DB_PASS,
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", conn);
        cmd.Parameters.AddWithValue("name", Env.DB_NAME!);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task CreateDatabase()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = Env.DB_HOST,
            Port = Convert.ToInt32(Env.DB_PORT),
            Username = Env.DB_USER,
            Password = Env.DB_PASS,
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{Env.DB_NAME}\"", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}