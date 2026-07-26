using System.Text.Json.Serialization;

namespace Scheder.JournalAPI;

public class AuthClass
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
    
    [JsonPropertyName("JWT")]
    public string? Jwt { get; set; }
    [JsonPropertyName("JWTRefreshTime")]
    public DateTime? JwtRefreshTime { get; set; }
}