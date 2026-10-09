using System.Windows;
using JimsFlightController.Services;
using JimsFlightController.ViewModels;

namespace MSFS2024Companion;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var simService = new SimConnectService(Dispatcher);
        _viewModel = new MainViewModel(simService);
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel?.Dispose();
        base.OnClosed(e);
    }
}
