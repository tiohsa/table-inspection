using System.Data;
using System.Text.Json;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Infrastructure.Repositories;

public class JsonPresetRepository : IPresetRepository
{
    private readonly string _filePath = "presets.json";

    public async Task SavePresetAsync(Preset preset)
    {
        List<Preset> presets = await LoadPresetsAsync();

        // Remove existing with same name if exists, or just append
        var existing = presets.FirstOrDefault(p => p.Name == preset.Name);
        if (existing != null)
        {
            presets.Remove(existing);
        }
        presets.Add(preset);

        var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<List<Preset>> LoadPresetsAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<Preset>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<Preset>>(json) ?? new List<Preset>();
        }
        catch
        {
            return new List<Preset>();
        }
    }
}
