using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Core.Interfaces;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync(ConnectionConfig config);
    Task<IEnumerable<string>> GetSchemasAsync(ConnectionConfig config);
    Task<IEnumerable<string>> GetTablesAsync(ConnectionConfig config, string schema);
    Task<IEnumerable<ColumnMetadata>> GetColumnsAsync(ConnectionConfig config, string schema, string table);

    // Returns the raw data for analysis based on the request constraints (Range, Limit)
    Task<IEnumerable<object>> GetDataAsync(AnalysisRequest request, CancellationToken cancellationToken);
}
