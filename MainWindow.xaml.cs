using System;
using System.Windows;
using TestProject1.ViewModels;

namespace TestProject1;

public partial class MainWindow : Window
{
    private SimulationWindow? _simulationWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = ((App)Application.Current).ViewModelFactory.CreateMainViewModel();
        Closed += OnClosed;
    }

    private void OpenSimulationWindow_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (_simulationWindow is { IsLoaded: true })
        {
            if (_simulationWindow.WindowState == WindowState.Minimized)
            {
                _simulationWindow.WindowState = WindowState.Normal;
            }

            _simulationWindow.Activate();
            return;
        }

        _simulationWindow = new SimulationWindow
        {
            Owner = this,
            DataContext = viewModel.SimulationPage
        };
        _simulationWindow.Closed += OnSimulationWindowClosed;
        _simulationWindow.Show();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_simulationWindow is not null)
        {
            _simulationWindow.Closed -= OnSimulationWindowClosed;
            _simulationWindow.Close();
            _simulationWindow = null;
        }

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void OnSimulationWindowClosed(object? sender, EventArgs e)
    {
        if (_simulationWindow is not null)
        {
            _simulationWindow.Closed -= OnSimulationWindowClosed;
            _simulationWindow = null;
        }
    }
}
