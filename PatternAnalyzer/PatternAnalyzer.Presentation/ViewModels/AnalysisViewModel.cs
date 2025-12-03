using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using System.Collections.ObjectModel;

namespace PatternAnalyzer.Presentation.ViewModels;

public partial class AnalysisViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;
    private readonly IPatternAnalyzer _analyzer;
    private readonly IPresetRepository _presetRepository;
    private readonly ConnectionViewModel _connectionVm;

    [ObservableProperty]
    private ObservableCollection<string> _schemas = new();

    [ObservableProperty]
    private string? _selectedSchema;

    [ObservableProperty]
    private ObservableCollection<string> _tables = new();

    [ObservableProperty]
    private string? _selectedTable;

    [ObservableProperty]
    private ObservableCollection<ColumnMetadata> _columns = new();

    [ObservableProperty]
    private ColumnMetadata? _selectedColumn;

    // Analysis Settings
    [ObservableProperty]
    private AnalysisType _selectedAnalysisType = AnalysisType.Auto;

    [ObservableProperty]
    private string _regexPattern = "";

    [ObservableProperty]
    private int _limit = 1000;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private AnalysisResult? _lastResult;

    [ObservableProperty]
    private string _presetName = "";

    private CancellationTokenSource? _cts;

    public AnalysisViewModel(
        IDatabaseService dbService,
        IPatternAnalyzer analyzer,
        IPresetRepository presetRepository,
        ConnectionViewModel connectionVm)
    {
        _dbService = dbService;
        _analyzer = analyzer;
        _presetRepository = presetRepository;
        _connectionVm = connectionVm;

        // In a real app, we'd use Messenger to react to Connection changes
    }

    [RelayCommand]
    public async Task LoadSchemasAsync()
    {
        if (!_connectionVm.IsConnected) return;

        try
        {
            var schemas = await _dbService.GetSchemasAsync(_connectionVm.CurrentConfig);
            Schemas = new ObservableCollection<string>(schemas);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading schemas: {ex.Message}";
        }
    }

    partial void OnSelectedSchemaChanged(string? value)
    {
        if (value != null)
        {
            LoadTablesAsync(value).ConfigureAwait(false);
        }
    }

    private async Task LoadTablesAsync(string schema)
    {
        try
        {
            var tables = await _dbService.GetTablesAsync(_connectionVm.CurrentConfig, schema);
            Tables = new ObservableCollection<string>(tables);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading tables: {ex.Message}";
        }
    }

    partial void OnSelectedTableChanged(string? value)
    {
        if (value != null && SelectedSchema != null)
        {
            LoadColumnsAsync(SelectedSchema, value).ConfigureAwait(false);
        }
    }

    private async Task LoadColumnsAsync(string schema, string table)
    {
        try
        {
            var cols = await _dbService.GetColumnsAsync(_connectionVm.CurrentConfig, schema, table);
            Columns = new ObservableCollection<ColumnMetadata>(cols);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading columns: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task RunAnalysisAsync()
    {
        if (SelectedColumn == null && SelectedAnalysisType != AnalysisType.Auto) // Auto might support full table in future, but assuming column for now
        {
            StatusMessage = "Please select a column.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Fetching data...";
        _cts = new CancellationTokenSource();

        try
        {
            var request = new AnalysisRequest
            {
                Connection = _connectionVm.CurrentConfig,
                Schema = SelectedSchema ?? "",
                Table = SelectedTable ?? "",
                Column = SelectedColumn?.ColumnName ?? "",
                Limit = Limit,
                AnalysisType = SelectedAnalysisType,
                RegexPattern = RegexPattern
            };

            var data = await _dbService.GetDataAsync(request, _cts.Token);

            StatusMessage = "Analyzing pattern...";
            // Run CPU bound work on background thread
            var result = await Task.Run(() => _analyzer.Analyze(data, request), _cts.Token);

            LastResult = result;
            StatusMessage = result.Success ? "Analysis Complete." : "Analysis Failed.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation Cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    public void CancelAnalysis()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    public async Task SavePresetAsync()
    {
        if (string.IsNullOrWhiteSpace(PresetName)) return;

        var request = new AnalysisRequest
        {
             Connection = _connectionVm.CurrentConfig,
             Schema = SelectedSchema ?? "",
             Table = SelectedTable ?? "",
             Column = SelectedColumn?.ColumnName ?? "",
             Limit = Limit,
             AnalysisType = SelectedAnalysisType,
             RegexPattern = RegexPattern
        };

        await _presetRepository.SavePresetAsync(request, PresetName);
        StatusMessage = $"Preset '{PresetName}' saved.";
    }
}
