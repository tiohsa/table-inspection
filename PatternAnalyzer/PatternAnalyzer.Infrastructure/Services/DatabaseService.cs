using Npgsql;
using Oracle.ManagedDataAccess.Client;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using System.Data;
using System.Data.Common;

namespace PatternAnalyzer.Infrastructure.Services;

public class DatabaseService : IDatabaseService
{
    private DbConnection CreateConnection(DbConnectionInfo info)
    {
        if (info.Type == DatabaseType.PostgreSQL)
        {
            return new NpgsqlConnection(info.GetConnectionString());
        }
        else
        {
            return new OracleConnection(info.GetConnectionString());
        }
    }

    public async Task<bool> TestConnectionAsync(DbConnectionInfo info, CancellationToken cancellationToken)
    {
        using var conn = CreateConnection(info);
        try
        {
            await conn.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<TableInfo>> GetTablesAsync(DbConnectionInfo info, CancellationToken cancellationToken)
    {
        var tables = new List<TableInfo>();
        using var conn = CreateConnection(info);
        await conn.OpenAsync(cancellationToken);

        string query;
        if (info.Type == DatabaseType.PostgreSQL)
        {
            query = "SELECT table_schema, table_name FROM information_schema.tables WHERE table_schema NOT IN ('information_schema', 'pg_catalog') ORDER BY table_schema, table_name LIMIT 1000";
        }
        else
        {
            query = "SELECT OWNER, TABLE_NAME FROM ALL_TABLES WHERE OWNER NOT IN ('SYS', 'SYSTEM') FETCH FIRST 1000 ROWS ONLY";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableInfo
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1)
            });
        }
        return tables;
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(DbConnectionInfo info, TableInfo table, CancellationToken cancellationToken)
    {
        var columns = new List<ColumnInfo>();
        using var conn = CreateConnection(info);
        await conn.OpenAsync(cancellationToken);

        string query;
        if (info.Type == DatabaseType.PostgreSQL)
        {
            query = "SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = @schema AND table_name = @table ORDER BY ordinal_position";
        }
        else
        {
            query = "SELECT COLUMN_NAME, DATA_TYPE FROM ALL_TAB_COLUMNS WHERE OWNER = :schema AND TABLE_NAME = :table ORDER BY COLUMN_ID";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = query;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = info.Type == DatabaseType.PostgreSQL ? "@schema" : ":schema";
        p1.Value = table.Schema;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = info.Type == DatabaseType.PostgreSQL ? "@table" : ":table";
        p2.Value = table.Name;
        cmd.Parameters.Add(p2);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                Table = table
            });
        }
        return columns;
    }

    public async Task<List<string>> SampleDataAsync(DbConnectionInfo info, ColumnInfo column, int limit, CancellationToken cancellationToken)
    {
        var data = new List<string>();
        using var conn = CreateConnection(info);
        await conn.OpenAsync(cancellationToken);

        // Quote identifiers to be safe
        string schema = QuoteIdentifier(column.Table.Schema, info.Type);
        string table = QuoteIdentifier(column.Table.Name, info.Type);
        string col = QuoteIdentifier(column.Name, info.Type);

        string query;
        if (info.Type == DatabaseType.PostgreSQL)
        {
            query = $"SELECT {col}::text FROM {schema}.{table} WHERE {col} IS NOT NULL ORDER BY {col} LIMIT {limit}";
        }
        else
        {
            // Oracle
            query = $"SELECT CAST({col} AS VARCHAR2(4000)) FROM {schema}.{table} WHERE {col} IS NOT NULL ORDER BY {col} FETCH FIRST {limit} ROWS ONLY";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = query;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                data.Add(reader.GetString(0));
            }
        }

        return data;
    }

    private string QuoteIdentifier(string id, DatabaseType type)
    {
        // Escape double quotes to prevent injection if identifier contains "
        string escaped = id.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
