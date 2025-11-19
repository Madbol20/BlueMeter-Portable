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
    private readonly DpsTrendChartView _dpsTrendChartView;
    private readonly SkillBreakdownChartView _skillBreakdownChartView;

    public ChartsWindow(
        ChartsWindowViewModel viewModel,
        DpsTrendChartView dpsTrendChartView,
        SkillBreakdownChartView skillBreakdownChartView)
    {
        _viewModel = viewModel;
        _dpsTrendChartView = dpsTrendChartView;
        _skillBreakdownChartView = skillBreakdownChartView;
        DataContext = _viewModel;

        InitializeComponent();

        // Inject the chart views into their respective tabs
        DpsTrendChartContainer.Content = _dpsTrendChartView;
        SkillBreakdownChartContainer.Content = _skillBreakdownChartView;

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

    /// <summary>
    /// Set the focused player for the charts
    /// </summary>
    public void SetFocusedPlayer(long? playerId)
    {
        _viewModel.SetFocusedPlayer(playerId);

        // Notify chart ViewModels
        if (_dpsTrendChartView.DataContext is DpsTrendChartViewModel dpsTrendViewModel)
        {
            dpsTrendViewModel.SetFocusedPlayer(playerId);
        }

        if (_skillBreakdownChartView.DataContext is SkillBreakdownChartViewModel skillBreakdownViewModel)
        {
            skillBreakdownViewModel.SetFocusedPlayer(playerId);
        }
    }
}
