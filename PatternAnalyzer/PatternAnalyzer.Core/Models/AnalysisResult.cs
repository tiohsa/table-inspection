namespace PatternAnalyzer.Core.Models;

public class AnalysisResult
{
    public string ColumnName { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public int MatchCount { get; set; }
    public double MatchPercentage => SampleSize == 0 ? 0 : (double)MatchCount / SampleSize * 100;
    public List<string> DetectedPatterns { get; set; } = new();
    public List<string> SampleValues { get; set; } = new();
    public bool IsMatch { get; set; }
}
