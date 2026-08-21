using Npgsql;
using Scheder.Tools.Config;

namespace Scheder.Services.Database;

public class DatabaseTools
{
    public const string DatabaseName = "AcademySched";
    
    public static readonly string ConnectionString = 
        $"Host={Env.DbHost};Port={Env.DbPort};Database={Env.DbName};Username={Env.DbUser};Password={Env.DbPass}";

    
    public static async Task<bool> DatabaseExists()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = Env.DbHost,
            Port = Convert.ToInt32(Env.DbPort),
            Username = Env.DbUser,
            Password = Env.DbPass,
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", conn);
        cmd.Parameters.AddWithValue("name", Env.DbName!);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task CreateDatabase()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = Env.DbHost,
            Port = Convert.ToInt32(Env.DbPort),
            Username = Env.DbUser,
            Password = Env.DbPass,
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{Env.DbName}\"", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}