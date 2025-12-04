using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using System.Collections.ObjectModel;

namespace PatternAnalyzer.Presentation.ViewModels;

public partial class AnalysisViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;
    private readonly IAnalysisService _analysisService;
    private readonly IPresetRepository _presetRepository;

    [ObservableProperty]
    private string _regexPattern = "";

    [ObservableProperty]
    private int _sampleSize = 100;

    [ObservableProperty]
    private string _presetName = "";

    [ObservableProperty]
    private AnalysisResult? _result;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<Preset> Presets { get; } = new();

    [ObservableProperty]
    private Preset? _selectedPreset;

    private ConnectionViewModel? _connectionViewModel;

    public AnalysisViewModel(
        IDatabaseService dbService,
        IAnalysisService analysisService,
        IPresetRepository presetRepository)
    {
        _dbService = dbService;
        _analysisService = analysisService;
        _presetRepository = presetRepository;
        LoadPresetsCommand.Execute(null);
    }

    public void Initialize(ConnectionViewModel connectionViewModel)
    {
        _connectionViewModel = connectionViewModel;
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (_connectionViewModel == null || _connectionViewModel.SelectedColumn == null)
            return;

        IsBusy = true;
        try
        {
            var info = _connectionViewModel.GetConnectionInfo();
            var col = _connectionViewModel.SelectedColumn;

            var data = await _dbService.SampleDataAsync(info, col, SampleSize, CancellationToken.None);

            var result = await _analysisService.AnalyzeColumnAsync(data, RegexPattern);
            var patterns = await _analysisService.DetectPatternsAsync(data);

            result.ColumnName = col.Name;
            result.DetectedPatterns = patterns;

            Result = result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        if (_connectionViewModel == null) return;

        var name = string.IsNullOrWhiteSpace(PresetName)
            ? $"Preset_{DateTime.Now:yyyyMMddHHmmss}"
            : PresetName;

        var preset = new Preset
        {
            Name = name,
            ConnectionInfo = _connectionViewModel.GetConnectionInfo(),
            RegexPattern = RegexPattern,
            SampleSize = SampleSize
        };

        await _presetRepository.SavePresetAsync(preset);
        await LoadPresetsAsync();
        PresetName = ""; // Reset
    }

    [RelayCommand]
    private async Task LoadPresetsAsync()
    {
        Presets.Clear();
        var list = await _presetRepository.LoadPresetsAsync();
        foreach (var p in list) Presets.Add(p);
    }

    partial void OnSelectedPresetChanged(Preset? value)
    {
        if (value == null) return;
        RegexPattern = value.RegexPattern;
        SampleSize = value.SampleSize;
        PresetName = value.Name;

        // Restore connection info
        if (_connectionViewModel != null && value.ConnectionInfo != null)
        {
            _connectionViewModel.LoadConnectionInfo(value.ConnectionInfo);
        }
    }
}
