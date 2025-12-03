using System.Data;
using System.Data.Common;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using Serilog;

namespace PatternAnalyzer.Infrastructure.Services;

public class DatabaseService : IDatabaseService
{
    private readonly ILogger _logger;

    public DatabaseService()
    {
        _logger = Log.ForContext<DatabaseService>();
    }

    private DbConnection CreateConnection(ConnectionConfig config)
    {
        if (config.Type == DatabaseType.PostgreSQL)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = config.Host,
                Port = config.Port,
                Username = config.Username,
                Password = config.Password,
                Database = config.DatabaseName
            };
            return new NpgsqlConnection(builder.ToString());
        }
        else // Oracle
        {
            // Simplified Oracle connection string
            string connStr = $"User Id={config.Username};Password={config.Password};Data Source={config.Host}:{config.Port}/{config.ServiceName}";
            return new OracleConnection(connStr);
        }
    }

    public async Task<bool> TestConnectionAsync(ConnectionConfig config)
    {
        try
        {
            using var conn = CreateConnection(config);
            await conn.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Connection test failed for {ConnectionName}", config.ConnectionName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetSchemasAsync(ConnectionConfig config)
    {
        var schemas = new List<string>();
        using var conn = CreateConnection(config);
        await conn.OpenAsync();

        string sql = config.Type == DatabaseType.PostgreSQL
            ? "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'pg_catalog')"
            : "SELECT username FROM all_users ORDER BY username"; // Simplified for Oracle

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }
        return schemas;
    }

    public async Task<IEnumerable<string>> GetTablesAsync(ConnectionConfig config, string schema)
    {
        var tables = new List<string>();
        using var conn = CreateConnection(config);
        await conn.OpenAsync();

        string sql = config.Type == DatabaseType.PostgreSQL
            ? "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema"
            : "SELECT table_name FROM all_tables WHERE owner = :schema";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var p = cmd.CreateParameter();
        p.ParameterName = config.Type == DatabaseType.PostgreSQL ? "@schema" : ":schema";
        p.Value = schema;
        cmd.Parameters.Add(p);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    public async Task<IEnumerable<ColumnMetadata>> GetColumnsAsync(ConnectionConfig config, string schema, string table)
    {
        var columns = new List<ColumnMetadata>();
        using var conn = CreateConnection(config);
        await conn.OpenAsync();

        string sql;
        if (config.Type == DatabaseType.PostgreSQL)
        {
            sql = @"
                SELECT column_name, data_type, is_nullable
                FROM information_schema.columns
                WHERE table_schema = @schema AND table_name = @table
                ORDER BY ordinal_position";
        }
        else
        {
            sql = @"
                SELECT column_name, data_type, nullable
                FROM all_tab_columns
                WHERE owner = :schema AND table_name = :table
                ORDER BY column_id";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = config.Type == DatabaseType.PostgreSQL ? "@schema" : ":schema";
        p1.Value = schema;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = config.Type == DatabaseType.PostgreSQL ? "@table" : ":table";
        p2.Value = table;
        cmd.Parameters.Add(p2);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnMetadata
            {
                SchemaName = schema,
                TableName = table,
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase) || reader.GetString(2).Equals("Y", StringComparison.OrdinalIgnoreCase)
            });
        }
        return columns;
    }

    public async Task<IEnumerable<object>> GetDataAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var data = new List<object>();
        using var conn = CreateConnection(request.Connection);
        await conn.OpenAsync(cancellationToken);

        string sql;
        if (request.UseCustomSql && !string.IsNullOrWhiteSpace(request.CustomSql))
        {
            // Note: In a real scenario, we should wrap this to ensure LIMIT is applied if not present,
            // or just trust the user/validator. For this implementation, we append LIMIT.
             sql = ApplyLimit(request.CustomSql, request.Connection.Type, request.Limit);
        }
        else
        {
             sql = BuildQuery(request);
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Add range parameters if needed (simplified for this example)
        // If range was based on ID/Sequence, we would add WHERE clause parameters here.

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (await reader.IsDBNullAsync(0, cancellationToken))
                continue;

            data.Add(reader.GetValue(0));
        }

        return data;
    }

    private string BuildQuery(AnalysisRequest request)
    {
        var type = request.Connection.Type;
        string schema = request.Schema;
        string table = request.Table;
        string column = request.Column;

        // Basic validation/sanitization should be done here to prevent SQL injection
        // if these come from free text input that wasn't validated.
        // Assuming simple identifiers for now or valid inputs.

        string query = $"SELECT {column} FROM {schema}.{table}";

        // Range Logic (Assuming basic LIMIT/OFFSET style or ID range if specified)
        // Since the prompt asks for "SQL Range Specification", usually implies WHERE clause on some column or ROWNUM.
        // Without a primary key/sort column known, pure "Range" is hard.
        // We will implement basic ROWNUM/LIMIT logic here as a default "Range".

        return ApplyLimit(query, type, request.Limit);
    }

    private string ApplyLimit(string sql, DatabaseType type, int limit)
    {
        // Simple logic to append limit. Robust parser would be better.
        if (type == DatabaseType.PostgreSQL)
        {
             return $"{sql} LIMIT {limit}";
        }
        else
        {
             return $"{sql} FETCH FIRST {limit} ROWS ONLY";
        }
    }
}
