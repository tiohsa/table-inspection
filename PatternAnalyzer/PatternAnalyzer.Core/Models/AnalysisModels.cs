namespace PatternAnalyzer.Core.Models;

public enum AnalysisType
{
    Prefix,
    Sequence,
    Regex,
    Auto
}

public class AnalysisRequest
{
    public ConnectionConfig Connection { get; set; } = new();
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string Column { get; set; } = string.Empty;

    public string CustomSql { get; set; } = string.Empty;
    public bool UseCustomSql { get; set; }

    public int? RangeStart { get; set; }
    public int? RangeEnd { get; set; }
    public int Limit { get; set; } = 1000;

    public AnalysisType AnalysisType { get; set; }
    public string? RegexPattern { get; set; }
}

public class AnalysisResult
{
    public string ColumnName { get; set; } = string.Empty;
    public AnalysisType AnalysisType { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRowsAnalyzed { get; set; }
    public int MatchCount { get; set; }
    public double MatchPercentage => TotalRowsAnalyzed == 0 ? 0 : (double)MatchCount / TotalRowsAnalyzed * 100;

    // Detailed findings
    public string? CommonPrefix { get; set; }
    public bool IsSequential { get; set; }
    public List<string> SampleViolations { get; set; } = new();
}
