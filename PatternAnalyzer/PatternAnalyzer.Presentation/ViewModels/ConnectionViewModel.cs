using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using System.Collections.ObjectModel;

namespace PatternAnalyzer.Presentation.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    public DatabaseType[] DatabaseTypes { get; } = (DatabaseType[])Enum.GetValues(typeof(DatabaseType));

    [ObservableProperty]
    private DatabaseType _selectedDatabaseType;

    [ObservableProperty]
    private string _host = "localhost";

    [ObservableProperty]
    private int _port = 5432;

    [ObservableProperty]
    private string _databaseName = "postgres";

    [ObservableProperty]
    private string _username = "postgres";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = "Not connected";

    public ObservableCollection<TableInfo> Tables { get; } = new();

    [ObservableProperty]
    private TableInfo? _selectedTable;

    public ObservableCollection<ColumnInfo> Columns { get; } = new();

    [ObservableProperty]
    private ColumnInfo? _selectedColumn;

    public ConnectionViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
        SelectedDatabaseType = DatabaseType.PostgreSQL;
    }

    public DbConnectionInfo GetConnectionInfo()
    {
        return new DbConnectionInfo
        {
            Type = SelectedDatabaseType,
            Host = Host,
            Port = Port,
            DatabaseName = DatabaseName,
            Username = Username,
            Password = Password
        };
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        StatusMessage = "Connecting...";
        var info = GetConnectionInfo();
        var success = await _dbService.TestConnectionAsync(info, CancellationToken.None);
        IsConnected = success;
        StatusMessage = success ? "Connected" : "Connection failed";

        if (success)
        {
            await LoadTablesAsync();
        }
    }

    private async Task LoadTablesAsync()
    {
        Tables.Clear();
        var info = GetConnectionInfo();
        var tables = await _dbService.GetTablesAsync(info, CancellationToken.None);
        foreach (var t in tables) Tables.Add(t);
    }

    async partial void OnSelectedTableChanged(TableInfo? value)
    {
        if (value == null) return;
        Columns.Clear();
        var info = GetConnectionInfo();
        var cols = await _dbService.GetColumnsAsync(info, value, CancellationToken.None);
        foreach (var c in cols) Columns.Add(c);
    }
}
