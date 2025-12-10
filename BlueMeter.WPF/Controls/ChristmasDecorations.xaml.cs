using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        StartSnowfall();
        StartTwinkling();
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
        if (SnowCanvas == null) return;

        try
        {
            // Random snowflake image
            var imageUri = new Uri(_snowflakeImages[_random.Next(_snowflakeImages.Length)], UriKind.Absolute);
            var snowflake = new Image
            {
                Source = new BitmapImage(imageUri),
                Width = _random.Next(12, 28),
                Height = _random.Next(12, 28),
                Opacity = 0
            };

            // Random horizontal position
            var left = _random.Next(0, (int)Math.Max(1, ActualWidth));
            Canvas.SetLeft(snowflake, left);
            Canvas.SetTop(snowflake, -50);

            SnowCanvas.Children.Add(snowflake);

            // Create animation with random duration
            var duration = TimeSpan.FromSeconds(_random.Next(10, 20));
            var fallAnimation = new DoubleAnimation
            {
                From = -50,
                To = ActualHeight + 50,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 0.7,
                Duration = TimeSpan.FromSeconds(2)
            };

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 0.7,
                To = 0,
                BeginTime = duration - TimeSpan.FromSeconds(2),
                Duration = TimeSpan.FromSeconds(2)
            };

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
            fallAnimation.Completed += (s, e) =>
            {
                SnowCanvas.Children.Remove(snowflake);
            };

            snowflake.BeginAnimation(Canvas.TopProperty, fallAnimation);
            snowflake.BeginAnimation(OpacityProperty, fadeInAnimation);
            snowflake.BeginAnimation(OpacityProperty, fadeOutAnimation);
            snowflake.BeginAnimation(Canvas.LeftProperty, swayAnimation);
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
}
