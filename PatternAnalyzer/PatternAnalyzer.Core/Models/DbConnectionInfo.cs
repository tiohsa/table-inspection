using System.Text.Json.Serialization;

namespace PatternAnalyzer.Core.Models;

public enum DatabaseType
{
    PostgreSQL,
    Oracle
}

public class DbConnectionInfo
{
    public DatabaseType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty; // Service Name for Oracle
    public string Username { get; set; } = string.Empty;

    // Do not serialize password
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    public string GetConnectionString()
    {
        if (Type == DatabaseType.PostgreSQL)
        {
            return $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}";
        }
        else
        {
            return $"User Id={Username};Password={Password};Data Source={Host}:{Port}/{DatabaseName}";
        }
    }
}
