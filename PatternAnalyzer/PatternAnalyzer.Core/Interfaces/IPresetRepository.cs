using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Core.Interfaces;

public interface IPresetRepository
{
    Task SavePresetAsync(Preset preset);
    Task<List<Preset>> LoadPresetsAsync();
}
