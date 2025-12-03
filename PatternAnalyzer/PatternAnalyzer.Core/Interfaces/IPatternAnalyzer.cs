using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Core.Interfaces;

public interface IPatternAnalyzer
{
    AnalysisResult Analyze(IEnumerable<object> data, AnalysisRequest request);
}

public interface IAnalysisStrategy
{
    bool CanHandle(AnalysisType type);
    AnalysisResult Execute(IEnumerable<object> data, AnalysisRequest request);
}
