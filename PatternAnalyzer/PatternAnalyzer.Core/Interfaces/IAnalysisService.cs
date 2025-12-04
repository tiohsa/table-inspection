using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Core.Interfaces;

public interface IAnalysisService
{
    Task<AnalysisResult> AnalyzeColumnAsync(List<string> data, string regexPattern);
    Task<List<string>> DetectPatternsAsync(List<string> data);
}
