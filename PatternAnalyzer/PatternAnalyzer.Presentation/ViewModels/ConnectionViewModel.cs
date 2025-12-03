using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using System.Collections.ObjectModel;

namespace PatternAnalyzer.Presentation.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private DatabaseType _selectedDatabaseType;

    [ObservableProperty]
    private string _host = "localhost";

    [ObservableProperty]
    private int _port = 5432;

    [ObservableProperty]
    private string _username = "postgres";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _databaseName = "postgres";

    [ObservableProperty]
    private string _serviceName = "ORCL";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = "";

    public ConnectionConfig CurrentConfig => new ConnectionConfig
    {
        Type = SelectedDatabaseType,
        Host = Host,
        Port = Port,
        Username = Username,
        Password = Password,
        DatabaseName = DatabaseName,
        ServiceName = ServiceName
    };

    public ConnectionViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        StatusMessage = "Connecting...";
        IsConnected = false;

        bool success = await _dbService.TestConnectionAsync(CurrentConfig);

        if (success)
        {
            StatusMessage = "Connected successfully!";
            IsConnected = true;
        }
        else
        {
            StatusMessage = "Connection failed.";
            IsConnected = false;
        }
    }
}
