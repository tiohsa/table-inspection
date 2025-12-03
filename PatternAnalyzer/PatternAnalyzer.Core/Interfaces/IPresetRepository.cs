using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Core.Interfaces;

public interface IPresetRepository
{
    Task SavePresetAsync(AnalysisRequest request, string name);
    Task<IEnumerable<(string Name, AnalysisRequest Request)>> LoadPresetsAsync();
}
