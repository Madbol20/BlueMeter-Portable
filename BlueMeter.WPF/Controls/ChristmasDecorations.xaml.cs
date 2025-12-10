using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private MediaPlayer? _musicPlayer;
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

        // Clean up music player
        _musicPlayer?.Stop();
        _musicPlayer?.Close();
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

    /// <summary>
    /// Handle bell click - play Christmas music
    /// </summary>
    private void ChristmasBell_Click(object sender, MouseButtonEventArgs e)
    {
        // Only play music if holiday themes are enabled
        if (!HolidayThemeService.IsHolidayActive())
            return;

        // Ensure we're on the UI thread for MediaPlayer
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ChristmasBell_Click(sender, e));
            return;
        }

        try
        {
            // 5% chance to play Carol of Bells instead (Easter egg!)
            string musicFileName = _random.Next(100) < 5
                ? "carolofbells_inst.mp3"
                : "jb_inst.mp3";

            // Get the music file path
            string musicPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "Themes", "Christmas", "music", musicFileName
            );

            if (!File.Exists(musicPath))
            {
                // Try alternate path
                musicPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets", "Themes", "Christmas", "music", musicFileName
                );
            }

            if (File.Exists(musicPath))
            {
                // Initialize MediaPlayer if needed
                if (_musicPlayer == null)
                {
                    _musicPlayer = new MediaPlayer();
                }
                else
                {
                    // Stop any currently playing music
                    _musicPlayer.Stop();
                }

                _musicPlayer.Volume = 0.15; // 15% volume
                _musicPlayer.Open(new Uri(musicPath, UriKind.Absolute));
                _musicPlayer.Play();

                // Trigger a stronger bell swing animation on click
                TriggerBellRing();
            }
        }
        catch (Exception ex)
        {
            // Log error for debugging
            System.Diagnostics.Debug.WriteLine($"Failed to play Christmas music: {ex.Message}");
        }
    }

    /// <summary>
    /// Trigger a more pronounced bell swing when clicked
    /// </summary>
    private void TriggerBellRing()
    {
        var bell = ChristmasBell;
        if (bell?.RenderTransform is RotateTransform rotateTransform)
        {
            var ringAnimation = new DoubleAnimation
            {
                From = -15,
                To = 15,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3) // Ring 3 times
            };

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, ringAnimation);
        }
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
