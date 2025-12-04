using System.Windows;
using BlueMeter.WPF.ViewModels;

namespace BlueMeter.WPF.Views;

/// <summary>
/// Combat Replay Window
/// Provides playback controls for recorded battle logs
/// </summary>
public partial class ReplayWindow : Window
{
    private readonly ReplayWindowViewModel _viewModel;

    public ReplayWindow(ReplayWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        // Subscribe to RequestClose event
        _viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose()
    {
        Close();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.RequestClose -= OnRequestClose;
    }
}
