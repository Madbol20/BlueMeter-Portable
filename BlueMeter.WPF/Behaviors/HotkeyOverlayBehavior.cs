using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlueMeter.WPF.Services;
using BlueMeter.WPF.ViewModels;
using Microsoft.Xaml.Behaviors;

namespace BlueMeter.WPF.Behaviors;

/// <summary>
/// Behavior to control overlay visibility for hotkey fields
/// Shows overlay when focused, hides when key is pressed
/// Also temporarily disables global hotkeys when focused to allow setting currently-registered hotkeys
/// </summary>
public class HotkeyOverlayBehavior : Behavior<TextBox>
{
    private bool _stoppedHotkeysForEditing;

    public static readonly DependencyProperty ShowOverlayProperty =
        DependencyProperty.Register(
            nameof(ShowOverlay),
            typeof(bool),
            typeof(HotkeyOverlayBehavior),
            new PropertyMetadata(false));

    public bool ShowOverlay
    {
        get => (bool)GetValue(ShowOverlayProperty);
        set => SetValue(ShowOverlayProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.GotFocus += OnGotFocus;
            AssociatedObject.LostFocus += OnLostFocus;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.GotFocus -= OnGotFocus;
            AssociatedObject.LostFocus -= OnLostFocus;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }
        base.OnDetaching();
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        ShowOverlay = true;

        // Temporarily stop global hotkeys when user clicks into hotkey field
        // This allows them to set currently-registered hotkeys (e.g., Ctrl+F6)
        var viewModel = AssociatedObject.DataContext as SettingsViewModel;
        if (viewModel != null && viewModel.AppConfig.GlobalHotkeysEnabled)
        {
            viewModel.GlobalHotkeyService.Stop();
            _stoppedHotkeysForEditing = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Hide overlay when any key is pressed
        ShowOverlay = false;
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        ShowOverlay = false;

        // Restart global hotkeys when user leaves the hotkey field
        // BUT only if the toggle is still enabled!
        if (_stoppedHotkeysForEditing)
        {
            var viewModel = AssociatedObject.DataContext as SettingsViewModel;
            if (viewModel != null && viewModel.AppConfig.GlobalHotkeysEnabled)
            {
                viewModel.GlobalHotkeyService.Start();
            }
            _stoppedHotkeysForEditing = false;
        }
    }
}
