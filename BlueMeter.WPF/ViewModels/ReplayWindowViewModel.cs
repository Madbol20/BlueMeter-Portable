using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BlueMeter.Core.Data;
using BlueMeter.Core.Data.Models;
using BlueMeter.Core.Analyze;
using BlueMeter.Core.Analyze.Models;
using Microsoft.Extensions.Logging;

namespace BlueMeter.WPF.ViewModels;

/// <summary>
/// ViewModel for Combat Replay Window
/// Provides playback controls for recorded battle logs
/// </summary>
public partial class ReplayWindowViewModel : ObservableObject
{
    private readonly ILogger<ReplayWindowViewModel> _logger;
    private readonly DispatcherTimer _playbackTimer;
    private BattleLogFileData[] _battleLogs = [];
    private Dictionary<long, PlayerInfoFileData> _playerInfos = new();
    private int _currentEventIndex = 0;
    private DateTime _replayStartTime;
    private long _firstEventTick;

    [ObservableProperty]
    private string _encounterTitle = "Combat Replay";

    [ObservableProperty]
    private ObservableCollection<ReplayEventViewModel> _events = new();

    [ObservableProperty]
    private ObservableCollection<ReplayEventViewModel> _filteredEvents = new();

    [ObservableProperty]
    private ReplayEventViewModel? _selectedEvent;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private double _currentTime;

    [ObservableProperty]
    private double _totalDuration;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private string _currentTimeText = "00:00";

    [ObservableProperty]
    private string _totalDurationText = "00:00";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    // Filter options
    [ObservableProperty]
    private bool _filterDamageEvents = true;

    [ObservableProperty]
    private bool _filterHealEvents = true;

    [ObservableProperty]
    private bool _filterCriticalOnly = false;

    [ObservableProperty]
    private bool _filterPlayerOnly = false;

    [ObservableProperty]
    private long? _selectedPlayerId;

    [ObservableProperty]
    private ObservableCollection<PlayerSelectionItem> _availablePlayers = new();

    [ObservableProperty]
    private int _totalEvents;

    [ObservableProperty]
    private int _filteredEventCount;

    public event Action? RequestClose;

    public ReplayWindowViewModel(ILogger<ReplayWindowViewModel> logger)
    {
        _logger = logger;

        // Playback timer (16ms = ~60 FPS)
        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _playbackTimer.Tick += OnPlaybackTick;
    }

    /// <summary>
    /// Load a combat log file for replay
    /// </summary>
    public async Task LoadCombatLogAsync(string filePath)
    {
        IsLoading = true;
        StatusText = "Loading combat log...";

        try
        {
            _logger.LogInformation("Loading combat log: {FilePath}", filePath);

            // Read BSON file
            await Task.Run(() =>
            {
                var fileData = BattleLogReader.ReadFile(filePath);
                _battleLogs = fileData.BattleLogs;
                _playerInfos = fileData.PlayerInfos.ToDictionary(p => p.UID, p => p);
            });

            if (_battleLogs.Length == 0)
            {
                MessageBox.Show("No battle events found in this log file.", "Empty Log", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Calculate duration
            _firstEventTick = _battleLogs[0].TimeTicks;
            var lastEventTick = _battleLogs[^1].TimeTicks;
            var durationTicks = lastEventTick - _firstEventTick;
            TotalDuration = durationTicks / 10_000.0; // Ticks to milliseconds
            TotalDurationText = FormatTime(TotalDuration);

            // Build event list
            await BuildEventListAsync();

            // Update player list
            UpdateAvailablePlayers();

            // Set title
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            EncounterTitle = $"Replay: {fileName}";

            TotalEvents = _battleLogs.Length;
            StatusText = $"Loaded {TotalEvents:N0} events • Duration: {TotalDurationText}";

            _logger.LogInformation("Loaded {EventCount} events, duration: {Duration}ms", _battleLogs.Length, TotalDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading combat log");
            MessageBox.Show($"Error loading combat log:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildEventListAsync()
    {
        await Task.Run(() =>
        {
            var events = new List<ReplayEventViewModel>();

            foreach (var log in _battleLogs)
            {
                var eventVm = new ReplayEventViewModel
                {
                    Timestamp = (log.TimeTicks - _firstEventTick) / 10_000.0, // ms
                    EventType = GetEventType(log),
                    AttackerName = GetPlayerName(log.AttackerUuid),
                    TargetName = GetPlayerName(log.TargetUuid),
                    Value = log.Value,
                    IsCritical = log.IsCritical,
                    IsLucky = log.IsLucky,
                    SkillId = log.SkillID,
                    Log = log
                };

                events.Add(eventVm);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Events = new ObservableCollection<ReplayEventViewModel>(events);
                ApplyFilters();
            });
        });
    }

    private void UpdateAvailablePlayers()
    {
        var players = _playerInfos.Values
            .Where(p => !string.IsNullOrEmpty(p.Name))
            .Select(p => new PlayerSelectionItem
            {
                PlayerId = p.UID,
                PlayerName = p.Name ?? $"Player {p.UID}"
            })
            .OrderBy(p => p.PlayerName)
            .ToList();

        AvailablePlayers = new ObservableCollection<PlayerSelectionItem>(players);
    }

    [RelayCommand]
    private void Play()
    {
        if (_battleLogs.Length == 0)
        {
            MessageBox.Show("No combat log loaded.", "Cannot Play", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsPlaying = true;
        IsPaused = false;
        _replayStartTime = DateTime.Now;

        // If at end, restart
        if (_currentEventIndex >= _battleLogs.Length - 1)
        {
            _currentEventIndex = 0;
            CurrentTime = 0;
        }

        _playbackTimer.Start();
        StatusText = $"Playing at {PlaybackSpeed}x speed";
        _logger.LogInformation("Playback started at {Speed}x speed", PlaybackSpeed);
    }

    [RelayCommand]
    private void Pause()
    {
        _playbackTimer.Stop();
        IsPlaying = false;
        IsPaused = true;
        StatusText = "Paused";
        _logger.LogInformation("Playback paused at {Time}ms", CurrentTime);
    }

    [RelayCommand]
    private void Stop()
    {
        _playbackTimer.Stop();
        IsPlaying = false;
        IsPaused = false;
        _currentEventIndex = 0;
        CurrentTime = 0;
        CurrentTimeText = "00:00";
        StatusText = "Stopped";
        _logger.LogInformation("Playback stopped");
    }

    [RelayCommand]
    private void SetSpeed(string speedString)
    {
        if (double.TryParse(speedString, out var speed))
        {
            PlaybackSpeed = speed;
            if (IsPlaying)
            {
                StatusText = $"Playing at {PlaybackSpeed}x speed";
            }
            _logger.LogInformation("Playback speed changed to {Speed}x", speed);
        }
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        if (Events == null || Events.Count == 0) return;

        var filtered = Events.AsEnumerable();

        // Filter by event type
        if (!FilterDamageEvents)
        {
            filtered = filtered.Where(e => e.EventType != "Damage");
        }
        if (!FilterHealEvents)
        {
            filtered = filtered.Where(e => e.EventType != "Heal");
        }

        // Filter critical only
        if (FilterCriticalOnly)
        {
            filtered = filtered.Where(e => e.IsCritical);
        }

        // Filter by player
        if (FilterPlayerOnly && SelectedPlayerId.HasValue)
        {
            filtered = filtered.Where(e =>
                e.Log.AttackerUuid == SelectedPlayerId.Value ||
                e.Log.TargetUuid == SelectedPlayerId.Value);
        }

        var filteredList = filtered.ToList();
        FilteredEvents = new ObservableCollection<ReplayEventViewModel>(filteredList);
        FilteredEventCount = filteredList.Count;

        _logger.LogDebug("Filters applied: {Count} events visible", FilteredEventCount);
    }

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (_battleLogs.Length == 0 || _currentEventIndex >= _battleLogs.Length)
        {
            Stop();
            StatusText = "Playback finished";
            return;
        }

        // Calculate elapsed time with speed multiplier
        var realElapsed = (DateTime.Now - _replayStartTime).TotalMilliseconds;
        var simulatedElapsed = realElapsed * PlaybackSpeed;

        // Update current time
        CurrentTime = simulatedElapsed;
        CurrentTimeText = FormatTime(CurrentTime);

        // Process events up to current time
        while (_currentEventIndex < _battleLogs.Length)
        {
            var log = _battleLogs[_currentEventIndex];
            var eventTime = (log.TimeTicks - _firstEventTick) / 10_000.0; // ms

            if (eventTime > simulatedElapsed)
            {
                break; // Haven't reached this event yet
            }

            // Process event (could update charts, stats, etc. here)
            _currentEventIndex++;
        }

        // Update status
        var progress = (CurrentTime / TotalDuration) * 100;
        StatusText = $"Playing at {PlaybackSpeed}x • {progress:F1}%";
    }

    [RelayCommand]
    private void SeekToTime(double milliseconds)
    {
        if (_battleLogs.Length == 0) return;

        var wasPlaying = IsPlaying;
        if (IsPlaying)
        {
            Pause();
        }

        CurrentTime = Math.Clamp(milliseconds, 0, TotalDuration);
        CurrentTimeText = FormatTime(CurrentTime);

        // Find event index for this time
        _currentEventIndex = 0;
        for (int i = 0; i < _battleLogs.Length; i++)
        {
            var eventTime = (_battleLogs[i].TimeTicks - _firstEventTick) / 10_000.0;
            if (eventTime > CurrentTime)
            {
                _currentEventIndex = i;
                break;
            }
        }

        if (wasPlaying)
        {
            _replayStartTime = DateTime.Now - TimeSpan.FromMilliseconds(CurrentTime / PlaybackSpeed);
        }

        _logger.LogDebug("Seeked to {Time}ms (event index: {Index})", CurrentTime, _currentEventIndex);
    }

    [RelayCommand]
    private void ExportEvents()
    {
        if (FilteredEvents.Count == 0)
        {
            MessageBox.Show("No events to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Events to CSV",
                FileName = "combat_events.csv",
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var csv = "Timestamp,EventType,Attacker,Target,Value,Critical,Lucky,SkillID\n";
                foreach (var evt in FilteredEvents)
                {
                    csv += $"{evt.Timestamp:F2},{evt.EventType},{evt.AttackerName},{evt.TargetName},{evt.Value},{evt.IsCritical},{evt.IsLucky},{evt.SkillId}\n";
                }

                System.IO.File.WriteAllText(dialog.FileName, csv);

                MessageBox.Show($"Exported {FilteredEvents.Count} events to:\n\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                _logger.LogInformation("Exported {Count} events to {Path}", FilteredEvents.Count, dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting events");
            MessageBox.Show($"Error exporting events:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Close()
    {
        Stop();
        RequestClose?.Invoke();
    }

    private static string FormatTime(double milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        if (ts.TotalHours >= 1)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }
        return ts.ToString(@"mm\:ss");
    }

    private string GetPlayerName(long playerId)
    {
        if (_playerInfos.TryGetValue(playerId, out var playerInfo))
        {
            return playerInfo.Name ?? $"Player {playerId}";
        }
        return $"Player {playerId}";
    }

    private static string GetEventType(BattleLogFileData log)
    {
        // Simplified event type detection
        // You can expand this based on actual game mechanics
        if (log.Value > 0)
        {
            return "Damage";
        }
        else if (log.Value < 0)
        {
            return "Heal";
        }
        return "Other";
    }

    partial void OnFilterDamageEventsChanged(bool value) => ApplyFilters();
    partial void OnFilterHealEventsChanged(bool value) => ApplyFilters();
    partial void OnFilterCriticalOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterPlayerOnlyChanged(bool value) => ApplyFilters();
    partial void OnSelectedPlayerIdChanged(long? value) => ApplyFilters();

    partial void OnCurrentTimeChanged(double value)
    {
        // Update slider position
        CurrentTimeText = FormatTime(value);
    }
}

/// <summary>
/// ViewModel for a single replay event
/// </summary>
public class ReplayEventViewModel
{
    public double Timestamp { get; init; } // milliseconds
    public string EventType { get; init; } = string.Empty;
    public string AttackerName { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public long Value { get; init; }
    public bool IsCritical { get; init; }
    public bool IsLucky { get; init; }
    public long SkillId { get; init; }
    public BattleLogFileData Log { get; init; }

    public string TimeText => FormatTime(Timestamp);
    public string ValueText => Value.ToString("N0");
    public string CriticalText => IsCritical ? "CRIT" : "";

    private static string FormatTime(double milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return ts.ToString(@"mm\:ss\.ff");
    }
}
