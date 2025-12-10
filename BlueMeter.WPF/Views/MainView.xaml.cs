using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using BlueMeter.WPF.Helpers;
using BlueMeter.WPF.Themes.SystemThemes;
using BlueMeter.WPF.ViewModels;

namespace BlueMeter.WPF.Views;

/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class MainView : Window
{
    private readonly MainViewModel _viewModel;
    private Cursor? _christmasCursor;
    private Cursor? _defaultCursor;

    public MainView(MainViewModel viewModel, SystemThemeWatcher watcher)
    {
        watcher.Watch(this);
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _defaultCursor = Cursor;

        Loaded += OnLoaded;
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                viewModel.MinimizeToTrayCommand.Execute(null);
            }
        };
        Closing += (s, e) =>
        {
            // default: hide instead of exit; user can Exit from tray menu
            e.Cancel = true;
            viewModel.MinimizeToTrayCommand.Execute(null);
        };

        // Subscribe to property changes for holiday theme toggle
        viewModel.AppConfig.PropertyChanged += AppConfig_PropertyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.InitializeTrayCommand.Execute(null);
        UpdateChristmasCursor();
    }

    private void AppConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.AppConfig.EnableHolidayThemes))
        {
            UpdateChristmasCursor();
        }
    }

    private void UpdateChristmasCursor()
    {
        if (_viewModel.AppConfig.EnableHolidayThemes)
        {
            // Create Christmas cursor if not already created
            if (_christmasCursor == null)
            {
                string cursorPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets", "Themes", "Christmas", "Decorations", "candy_cane.png"
                );

                _christmasCursor = CursorHelper.CreateCursorFromImage(cursorPath, 0, 0, -15); // -15° = tilt left
            }

            // Set Christmas cursor
            if (_christmasCursor != null)
            {
                Cursor = _christmasCursor;
            }
        }
        else
        {
            // Restore default cursor
            Cursor = _defaultCursor ?? Cursors.Arrow;
        }
    }

    public bool IsDebugContentVisible { get; } =
#if DEBUG
        true;
#else
        false;
#endif

    private void Footer_OnConfirmClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    // MEMORY LEAK FIX: Ensure ViewModel is disposed when window is actually closed.
    // Without this, the Dispose() method we added to MainViewModel would never be called,
    // and the CultureChanged event subscription would never be cleaned up.
    // Note: This is only called on actual application shutdown, not when minimizing to tray.
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.AppConfig.PropertyChanged -= AppConfig_PropertyChanged;
            viewModel.Dispose();
        }

        base.OnClosed(e);
    }

}
