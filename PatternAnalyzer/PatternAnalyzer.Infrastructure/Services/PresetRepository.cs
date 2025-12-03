using System.Text.Json;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Infrastructure.Services;

public class PresetRepository : IPresetRepository
{
    private readonly string _filePath;

    public PresetRepository(string filePath = "presets.json")
    {
        _filePath = filePath;
    }

    public async Task SavePresetAsync(AnalysisRequest request, string name)
    {
        var presets = (await LoadPresetsAsync()).ToList();

        // Remove existing if overwrite
        presets.RemoveAll(p => p.Name == name);
        presets.Add((name, request));

        var wrapper = presets.Select(p => new PresetWrapper { Name = p.Name, Request = p.Request });

        string json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<IEnumerable<(string Name, AnalysisRequest Request)>> LoadPresetsAsync()
    {
        if (!File.Exists(_filePath))
            return new List<(string Name, AnalysisRequest Request)>();

        try
        {
            string json = await File.ReadAllTextAsync(_filePath);
            var wrappers = JsonSerializer.Deserialize<List<PresetWrapper>>(json);
            return wrappers?.Select(w => (w.Name, w.Request)) ?? new List<(string, AnalysisRequest)>();
        }
        catch
        {
            return new List<(string Name, AnalysisRequest Request)>();
        }
    }

    private class PresetWrapper
    {
        public string Name { get; set; } = string.Empty;
        public AnalysisRequest Request { get; set; } = new();
    }
}
