using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TestProject1;

public partial class SimulationWindow : Window, INotifyPropertyChanged
{
    private bool _isDetailPaneVisible;

    public SimulationWindow()
    {
        InitializeComponent();
        _isDetailPaneVisible = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsDetailPaneVisible
    {
        get => _isDetailPaneVisible;
        private set
        {
            if (_isDetailPaneVisible == value)
            {
                return;
            }

            _isDetailPaneVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailToggleText));
        }
    }

    public string DetailToggleText => IsDetailPaneVisible ? "Hide Details / 隐藏详情" : "Show Details / 显示详情";

    public string WindowModeToggleText => WindowState == WindowState.Maximized ? "Windowed / 窗口化" : "Full Screen / 全屏";

    private void ToggleDetails_Click(object sender, RoutedEventArgs e)
    {
        IsDetailPaneVisible = !IsDetailPaneVisible;
    }

    private void ToggleWindowMode_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        OnPropertyChanged(nameof(WindowModeToggleText));
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        OnPropertyChanged(nameof(WindowModeToggleText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
