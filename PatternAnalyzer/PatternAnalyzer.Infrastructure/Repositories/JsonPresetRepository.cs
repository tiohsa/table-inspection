using System.Text.Json;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Infrastructure.Repositories;

public class JsonPresetRepository : IPresetRepository
{
    private string GetFilePath()
    {
        // Use AppDataDirectory for cross-platform support
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // In MAUI/Android/iOS context, Environment.SpecialFolder.LocalApplicationData usually maps correctly
        // to a writable private storage.
        // Or better, let the consumer inject the path, but for now:
        return Path.Combine(folder, "presets.json");
    }

    public async Task SavePresetAsync(Preset preset)
    {
        List<Preset> presets = await LoadPresetsAsync();

        var existing = presets.FirstOrDefault(p => p.Name == preset.Name);
        if (existing != null)
        {
            presets.Remove(existing);
        }
        presets.Add(preset);

        var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(GetFilePath(), json);
    }

    public async Task<List<Preset>> LoadPresetsAsync()
    {
        string path = GetFilePath();
        if (!File.Exists(path))
        {
            return new List<Preset>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<Preset>>(json) ?? new List<Preset>();
        }
        catch
        {
            return new List<Preset>();
        }
    }
}
