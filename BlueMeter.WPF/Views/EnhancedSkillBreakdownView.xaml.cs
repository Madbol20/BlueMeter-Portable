using System.Windows.Controls;
using BlueMeter.WPF.ViewModels;
using Microsoft.Extensions.Logging;

namespace BlueMeter.WPF.Views;

/// <summary>
/// Enhanced Skill Breakdown View with full StarResonanceDps-style stats
/// Includes Lucky Hits tracking and detailed skill breakdown
/// </summary>
public partial class EnhancedSkillBreakdownView : UserControl
{
    private readonly EnhancedSkillBreakdownViewModel _viewModel;
    private readonly ILogger<EnhancedSkillBreakdownView> _logger;

    public EnhancedSkillBreakdownView(
        EnhancedSkillBreakdownViewModel viewModel,
        ILogger<EnhancedSkillBreakdownView> logger)
    {
        _viewModel = viewModel;
        _logger = logger;

        InitializeComponent();

        // CRITICAL: Set DataContext!
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        _logger.LogInformation("EnhancedSkillBreakdownView created and DataContext set");
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _logger.LogInformation("EnhancedSkillBreakdownView loaded, calling ViewModel.OnViewLoaded()");
        _viewModel.OnViewLoaded();
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _logger.LogInformation("EnhancedSkillBreakdownView unloaded");
        _viewModel.OnViewUnloaded();
    }
}
