using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BlueMeter.WPF.Services;

namespace BlueMeter.WPF.Controls;

/// <summary>
/// Christmas decorations control with falling snow and twinkling lights
/// </summary>
public partial class ChristmasDecorations : UserControl
{
    private readonly Random _random = new();
    private DispatcherTimer? _snowTimer;
    private readonly string[] _snowflakeImages = {
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_01.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_02.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_03.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_04.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_05.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_06.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_07.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_08.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_small_09.png",
        "pack://application:,,,/Assets/Themes/Christmas/Snowflakes/snowflake_medium_01.png"
    };

    public ChristmasDecorations()
    {
        InitializeComponent();
        Loaded += ChristmasDecorations_Loaded;
        Unloaded += ChristmasDecorations_Unloaded;
    }

    private void ChristmasDecorations_Loaded(object sender, RoutedEventArgs e)
    {
        // Only start animations if currently in a holiday period
        if (HolidayThemeService.IsHolidayActive())
        {
            StartSnowfall();
            StartTwinkling();
        }
    }

    private void ChristmasDecorations_Unloaded(object sender, RoutedEventArgs e)
    {
        StopSnowfall();
    }

    private void StartSnowfall()
    {
        // Create snowflakes every 500ms
        _snowTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _snowTimer.Tick += (s, e) => CreateSnowflake();
        _snowTimer.Start();

        // Create initial batch of snowflakes
        for (int i = 0; i < 10; i++)
        {
            CreateSnowflake();
        }
    }

    private void StopSnowfall()
    {
        _snowTimer?.Stop();
        _snowTimer = null;
    }

    private void CreateSnowflake()
    {
        // Try to use MainSnowCanvas from parent window if available
        var window = Window.GetWindow(this);
        Canvas? snowCanvas = null;

        if (window != null)
        {
            // Search for MainSnowCanvas in the visual tree
            snowCanvas = FindVisualChild<Canvas>(window, "MainSnowCanvas");
        }

        snowCanvas ??= SnowCanvas;

        if (snowCanvas == null) return;

        try
        {
            // Get the actual window dimensions
            var targetHeight = window?.ActualHeight ?? 600;
            var targetWidth = window?.ActualWidth ?? 800;

            // Random snowflake image
            var imageUri = new Uri(_snowflakeImages[_random.Next(_snowflakeImages.Length)], UriKind.Absolute);
            var snowflake = new Image
            {
                Source = new BitmapImage(imageUri),
                Width = _random.Next(12, 28),
                Height = _random.Next(12, 28),
                Opacity = 0
            };

            // Random horizontal position across full width
            var left = _random.Next(0, (int)Math.Max(1, targetWidth));
            Canvas.SetLeft(snowflake, left);
            // Start from way above the window
            Canvas.SetTop(snowflake, -200);

            snowCanvas.Children.Add(snowflake);

            // Create animation with random duration
            var duration = TimeSpan.FromSeconds(_random.Next(10, 20));
            var fallAnimation = new DoubleAnimation
            {
                // Start from way above to ensure coverage from top
                From = -200,
                To = targetHeight + 100,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // Simple fade in at the start, stay visible, fade out at the end
            var storyboard = new Storyboard();

            var opacityAnimation = new DoubleAnimationUsingKeyFrames();
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0.7, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0.7, KeyTime.FromTimeSpan(duration - TimeSpan.FromSeconds(2))));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(duration)));

            Storyboard.SetTarget(opacityAnimation, snowflake);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(opacityAnimation);

            // Optional: gentle sway animation
            var swayAnimation = new DoubleAnimation
            {
                From = left - 30,
                To = left + 30,
                Duration = TimeSpan.FromSeconds(4),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            // Remove snowflake after animation completes
            storyboard.Completed += (s, e) =>
            {
                snowCanvas.Children.Remove(snowflake);
            };

            snowflake.BeginAnimation(Canvas.TopProperty, fallAnimation);
            snowflake.BeginAnimation(Canvas.LeftProperty, swayAnimation);
            storyboard.Begin();
        }
        catch
        {
            // Ignore errors (e.g., if control is being unloaded)
        }
    }

    private void StartTwinkling()
    {
        if (ChristmasLights == null) return;

        var twinkleStoryboard = (Storyboard)Resources["TwinkleAnimation"];
        Storyboard.SetTarget(twinkleStoryboard, ChristmasLights);
        twinkleStoryboard.Begin();
    }

    /// <summary>
    /// Helper method to find a named child in the visual tree
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent == null) return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild && typedChild.Name == name)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
