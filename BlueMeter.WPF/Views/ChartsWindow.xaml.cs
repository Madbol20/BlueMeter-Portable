using System.Windows;
using BlueMeter.WPF.ViewModels;

namespace BlueMeter.WPF.Views;

/// <summary>
/// Advanced Combat Log - Charts Window
/// Displays real-time DPS/HPS charts and analytics
/// </summary>
public partial class ChartsWindow : Window
{
    private readonly ChartsWindowViewModel _viewModel;

    public ChartsWindow(ChartsWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.OnWindowLoaded();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.OnWindowClosing();
    }
}
