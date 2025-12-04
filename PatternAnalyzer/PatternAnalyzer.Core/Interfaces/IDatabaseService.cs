using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Core.Interfaces;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync(DbConnectionInfo info, CancellationToken cancellationToken);
    Task<List<TableInfo>> GetTablesAsync(DbConnectionInfo info, CancellationToken cancellationToken);
    Task<List<ColumnInfo>> GetColumnsAsync(DbConnectionInfo info, TableInfo table, CancellationToken cancellationToken);
    Task<List<string>> SampleDataAsync(DbConnectionInfo info, ColumnInfo column, int limit, CancellationToken cancellationToken);
}
