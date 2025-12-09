using System.Windows;
using System.Windows.Input;
using BlueMeter.WPF.ViewModels;

namespace BlueMeter.WPF.Views;

/// <summary>
/// Combat Logs Management Window
/// Displays and manages stored BSON combat logs
/// </summary>
public partial class CombatLogsWindow : Window
{
    private readonly CombatLogsWindowViewModel _viewModel;

    public CombatLogsWindow(CombatLogsWindowViewModel viewModel)
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

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click to view details
        if (_viewModel.SelectedLogItem != null)
        {
            _viewModel.ViewDetailsCommand.Execute(null);
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.RequestClose -= OnRequestClose;
    }
}
