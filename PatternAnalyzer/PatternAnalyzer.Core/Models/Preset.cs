namespace PatternAnalyzer.Core.Models;

public class Preset
{
    public string Name { get; set; } = string.Empty;
    public DbConnectionInfo ConnectionInfo { get; set; } = new();
    public string RegexPattern { get; set; } = string.Empty;
    public int SampleSize { get; set; } = 100;
}
