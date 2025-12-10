using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlueMeter.WPF.Services;

namespace BlueMeter.WPF.Controls;

/// <summary>
/// Christmas decorations specifically for the DPS meter window
/// Includes frost border, festive background, and sparkle effects
/// </summary>
public partial class DpsMeterChristmasDecorations : UserControl
{
    private readonly Random _random = new();
    private DispatcherTimer? _sparkleTimer;

    public DpsMeterChristmasDecorations()
    {
        InitializeComponent();
        Loaded += DpsMeterChristmasDecorations_Loaded;
        Unloaded += DpsMeterChristmasDecorations_Unloaded;
    }

    private void DpsMeterChristmasDecorations_Loaded(object sender, RoutedEventArgs e)
    {
        // Only start animations if currently in a holiday period
        if (HolidayThemeService.IsHolidayActive())
        {
            StartSparkles();
        }
    }

    private void DpsMeterChristmasDecorations_Unloaded(object sender, RoutedEventArgs e)
    {
        StopSparkles();
    }

    private void StartSparkles()
    {
        // Create sparkles every 2 seconds
        _sparkleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _sparkleTimer.Tick += (s, e) => CreateSparkle();
        _sparkleTimer.Start();
    }

    private void StopSparkles()
    {
        _sparkleTimer?.Stop();
        _sparkleTimer = null;
    }

    private void CreateSparkle()
    {
        if (SparkleCanvas == null) return;

        try
        {
            var sparkle = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Themes/Christmas/Effects/sparkle.png", UriKind.Absolute)),
                Width = _random.Next(8, 16),
                Height = _random.Next(8, 16),
                Opacity = 0,
                RenderTransform = new ScaleTransform(0.5, 0.5)
            };

            // Random position
            var left = _random.Next(0, (int)Math.Max(1, ActualWidth));
            var top = _random.Next(0, (int)Math.Max(1, ActualHeight));
            Canvas.SetLeft(sparkle, left);
            Canvas.SetTop(sparkle, top);

            SparkleCanvas.Children.Add(sparkle);

            // Create sparkle animation
            var storyboard = new Storyboard();

            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 0.8,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true
            };

            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.5,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true
            };

            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.5,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true
            };

            Storyboard.SetTarget(opacityAnimation, sparkle);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

            Storyboard.SetTarget(scaleXAnimation, sparkle);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));

            Storyboard.SetTarget(scaleYAnimation, sparkle);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));

            storyboard.Children.Add(opacityAnimation);
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);

            // Remove sparkle after animation
            storyboard.Completed += (s, e) =>
            {
                SparkleCanvas.Children.Remove(sparkle);
            };

            storyboard.Begin();
        }
        catch
        {
            // Ignore errors
        }
    }
}
