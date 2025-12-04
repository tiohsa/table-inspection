using CommunityToolkit.Mvvm.ComponentModel;

namespace PatternAnalyzer.Presentation.ViewModels;

public class MainViewModel : ObservableObject
{
    public ConnectionViewModel ConnectionViewModel { get; }
    public AnalysisViewModel AnalysisViewModel { get; }

    public MainViewModel(ConnectionViewModel connectionViewModel, AnalysisViewModel analysisViewModel)
    {
        ConnectionViewModel = connectionViewModel;
        AnalysisViewModel = analysisViewModel;

        // Link them
        AnalysisViewModel.Initialize(ConnectionViewModel);
    }
}
