using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using BlueMeter.WPF.Config;
using BlueMeter.WPF.Models;
using Microsoft.Extensions.Logging;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Service for playing queue pop alert sounds
/// </summary>
public interface ISoundPlayerService : IDisposable
{
    /// <summary>
    /// Play the configured queue pop sound
    /// </summary>
    void PlayQueuePopSound();

    /// <summary>
    /// Test a specific sound with given volume
    /// </summary>
    void TestSound(QueuePopSound sound, double volume);
}

/// <summary>
/// Implementation of sound player service using MediaPlayer
/// </summary>
public sealed class SoundPlayerService : ISoundPlayerService
{
    private readonly ILogger<SoundPlayerService> _logger;
    private readonly IConfigManager _configManager;
    private readonly Dispatcher _dispatcher;
    private readonly MediaPlayer _mediaPlayer;
    private readonly object _playerLock = new();
    private bool _disposed;

    // Sound file paths relative to application directory
    private static readonly string SoundsDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Assets",
        "sounds"
    );

    public SoundPlayerService(
        ILogger<SoundPlayerService> logger,
        IConfigManager configManager,
        Dispatcher dispatcher)
    {
        _logger = logger;
        _configManager = configManager;
        _dispatcher = dispatcher;
        _mediaPlayer = new MediaPlayer();

        _logger.LogDebug("SoundPlayerService initialized. Sounds directory: {Directory}", SoundsDirectory);
    }

    public void PlayQueuePopSound()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SoundPlayerService));

        var config = _configManager.CurrentConfig;

        if (!config.QueuePopSoundEnabled)
        {
            _logger.LogDebug("Queue pop sound is disabled");
            return;
        }

        PlaySound(config.QueuePopSound, config.QueuePopSoundVolume);
    }

    public void TestSound(QueuePopSound sound, double volume)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SoundPlayerService));

        PlaySound(sound, volume);
    }

    private void PlaySound(QueuePopSound sound, double volume)
    {
        // Ensure we're on the UI thread for MediaPlayer
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => PlaySound(sound, volume));
            return;
        }

        try
        {
            lock (_playerLock)
            {
                var soundFile = GetSoundFilePath(sound);

                if (!File.Exists(soundFile))
                {
                    _logger.LogWarning("Sound file not found: {File}", soundFile);
                    return;
                }

                // Stop any currently playing sound
                _mediaPlayer.Stop();

                // Load and play the sound
                _mediaPlayer.Open(new Uri(soundFile, UriKind.Absolute));
                _mediaPlayer.Volume = Math.Clamp(volume / 100.0, 0.0, 1.0);
                _mediaPlayer.Play();

                _logger.LogDebug("Playing sound: {Sound} at {Volume}% volume", sound, volume);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sound: {Sound}", sound);
        }
    }

    private static string GetSoundFilePath(QueuePopSound sound)
    {
        var fileName = sound switch
        {
            QueuePopSound.Drum => "drum.mp3",
            QueuePopSound.Harp => "harp.mp3",
            QueuePopSound.Wow => "wow.mp3",
            QueuePopSound.Yoooo => "yoooo.mp3",
            _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, "Unknown sound type")
        };

        return Path.Combine(SoundsDirectory, fileName);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_playerLock)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
        }

        _logger.LogDebug("SoundPlayerService disposed");
    }
}
