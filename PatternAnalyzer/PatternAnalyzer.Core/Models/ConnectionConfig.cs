namespace PatternAnalyzer.Core.Models;

public enum DatabaseType
{
    PostgreSQL,
    Oracle
}

public class ConnectionConfig
{
    public DatabaseType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty; // For Oracle

    public string ConnectionName { get; set; } = "New Connection";
}
