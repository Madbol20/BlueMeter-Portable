using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BlueMeter.Core.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BlueMeter.WPF.ViewModels;

/// <summary>
/// ViewModel for Combat Logs Management Window
/// Displays and manages stored BSON combat logs
/// </summary>
public partial class CombatLogsWindowViewModel : ObservableObject
{
    private readonly ILogger<CombatLogsWindowViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<CombatLogItemViewModel> _logItems = new();

    [ObservableProperty]
    private CombatLogItemViewModel? _selectedLogItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _totalDiskUsage = "0 B";

    [ObservableProperty]
    private int _totalEncounters;

    [ObservableProperty]
    private int _maxEncounters;

    public event Action? RequestClose;

    public CombatLogsWindowViewModel(
        ILogger<CombatLogsWindowViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await RefreshLogsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RefreshLogsAsync();
    }

    private async Task RefreshLogsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading combat logs...";

        try
        {
            var manager = DataStorageExtensions.GetBattleLogManager();
            if (manager == null)
            {
                StatusMessage = "Advanced Combat Logging is not enabled";
                MessageBox.Show(
                    "Advanced Combat Logging is not currently enabled.\n\n" +
                    "To enable it:\n" +
                    "1. Go to Settings\n" +
                    "2. Enable 'Advanced Combat Logging'\n" +
                    "3. Restart BlueMeter",
                    "Feature Not Enabled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            await Task.Run(() =>
            {
                var encounters = manager.GetStoredEncounters();
                var totalSize = manager.GetTotalDiskUsageBytes();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogItems.Clear();
                    foreach (var encounter in encounters)
                    {
                        LogItems.Add(new CombatLogItemViewModel(encounter));
                    }

                    TotalEncounters = encounters.Count;
                    MaxEncounters = manager.MaxEncounters;
                    TotalDiskUsage = FormatBytes(totalSize);
                    StatusMessage = $"Loaded {encounters.Count} combat log(s) • {FormatBytes(totalSize)}";
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading combat logs");
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show(
                $"An error occurred while loading combat logs:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            var manager = DataStorageExtensions.GetBattleLogManager();
            if (manager == null)
            {
                MessageBox.Show("Advanced Combat Logging is not enabled.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var directory = manager.LogDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });

            _logger.LogInformation("Opened combat logs folder: {Directory}", directory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening logs folder");
            MessageBox.Show($"Error opening folder:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportSelectedAsync()
    {
        if (SelectedLogItem == null)
        {
            MessageBox.Show("Please select a log to export.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var manager = DataStorageExtensions.GetBattleLogManager();
            if (manager == null) return;

            var sourceFile = Path.Combine(manager.LogDirectory, SelectedLogItem.FileName);
            if (!File.Exists(sourceFile))
            {
                MessageBox.Show("Log file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Use SaveFileDialog
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Combat Log",
                FileName = SelectedLogItem.FileName,
                DefaultExt = ".bmlogs",
                Filter = "BlueMeter Logs (*.bmlogs)|*.bmlogs|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                await Task.Run(() => File.Copy(sourceFile, dialog.FileName, overwrite: true));

                _logger.LogInformation("Exported combat log: {FileName} -> {Destination}", SelectedLogItem.FileName, dialog.FileName);
                StatusMessage = $"Exported: {SelectedLogItem.FileName}";
                MessageBox.Show(
                    $"Log exported successfully!\n\n{dialog.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting log");
            MessageBox.Show($"Error exporting log:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportAllAsync()
    {
        if (LogItems.Count == 0)
        {
            MessageBox.Show("No logs to export.", "No Logs", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Export all {LogItems.Count} combat log(s) to a selected folder?",
            "Export All Logs",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var manager = DataStorageExtensions.GetBattleLogManager();
            if (manager == null) return;

            // Use FolderBrowserDialog
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select destination folder for combat logs",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IsLoading = true;
                StatusMessage = "Exporting logs...";

                await Task.Run(() =>
                {
                    int exportedCount = 0;
                    foreach (var logItem in LogItems)
                    {
                        var sourceFile = Path.Combine(manager.LogDirectory, logItem.FileName);
                        var destFile = Path.Combine(dialog.SelectedPath, logItem.FileName);

                        if (File.Exists(sourceFile))
                        {
                            File.Copy(sourceFile, destFile, overwrite: true);
                            exportedCount++;
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Exported {exportedCount} log(s)";
                    });
                });

                _logger.LogInformation("Exported all combat logs to: {Directory}", dialog.SelectedPath);
                MessageBox.Show(
                    $"Exported {LogItems.Count} log(s) successfully!\n\n{dialog.SelectedPath}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all logs");
            MessageBox.Show($"Error exporting logs:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedLogItem == null)
        {
            MessageBox.Show("Please select a log to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete this combat log?\n\n{SelectedLogItem.DisplayName}\n\nThis action cannot be undone.",
            "Delete Combat Log",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var manager = DataStorageExtensions.GetBattleLogManager();
            if (manager == null) return;

            await manager.DeleteEncounterAsync(SelectedLogItem.FileName);

            _logger.LogInformation("Deleted combat log: {FileName}", SelectedLogItem.FileName);
            StatusMessage = $"Deleted: {SelectedLogItem.FileName}";

            // Refresh list
            await RefreshLogsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting log");
            MessageBox.Show($"Error deleting log:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteAllAsync()
    {
        if (LogItems.Count == 0)
        {
            MessageBox.Show("No logs to delete.", "No Logs", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"⚠️ WARNING ⚠️\n\n" +
            $"This will delete ALL {LogItems.Count} combat log(s)!\n\n" +
            "This action CANNOT be undone.\n\n" +
            "Are you absolutely sure?",
            "Delete All Logs",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var manager = DataStorageExtensions.GetBattleLogManager();
            if (manager == null) return;

            IsLoading = true;
            StatusMessage = "Deleting all logs...";

            await manager.DeleteAllEncountersAsync();

            _logger.LogInformation("Deleted all combat logs");
            StatusMessage = "All logs deleted";

            MessageBox.Show(
                $"Successfully deleted {LogItems.Count} combat log(s).",
                "Logs Deleted",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Refresh list
            await RefreshLogsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all logs");
            MessageBox.Show($"Error deleting logs:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewReplayAsync()
    {
        if (SelectedLogItem == null)
        {
            MessageBox.Show("Please select a log to replay.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var replayWindow = _serviceProvider.GetService<Views.ReplayWindow>();
            if (replayWindow != null && replayWindow.DataContext is ReplayWindowViewModel replayViewModel)
            {
                // Load the combat log
                await replayViewModel.LoadCombatLogAsync(SelectedLogItem.FullPath);

                replayWindow.Owner = Application.Current.MainWindow;
                replayWindow.Show();

                _logger.LogInformation("Opened replay window for: {FileName}", SelectedLogItem.FileName);
            }
            else
            {
                _logger.LogError("Failed to create ReplayWindow");
                MessageBox.Show("Failed to open replay window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening replay window");
            MessageBox.Show($"Error opening replay:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ViewDetails()
    {
        if (SelectedLogItem == null)
        {
            MessageBox.Show("Please select a log to view.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var details = $"Combat Log Details:\n\n" +
                     $"Boss/Target: {SelectedLogItem.BossName}\n" +
                     $"Encounter ID: {SelectedLogItem.EncounterId}\n" +
                     $"Timestamp: {SelectedLogItem.FormattedDate}\n" +
                     $"File Size: {SelectedLogItem.FormattedSize}\n" +
                     $"File Name: {SelectedLogItem.FileName}\n\n" +
                     $"Storage Path:\n{SelectedLogItem.FullPath}";

        MessageBox.Show(details, "Combat Log Details", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// ViewModel for individual combat log item in the list
/// </summary>
public class CombatLogItemViewModel : ObservableObject
{
    private readonly EncounterFileInfo _fileInfo;

    public CombatLogItemViewModel(EncounterFileInfo fileInfo)
    {
        _fileInfo = fileInfo;

        // Get full path
        var manager = DataStorageExtensions.GetBattleLogManager();
        if (manager != null)
        {
            FullPath = Path.Combine(manager.LogDirectory, fileInfo.FileName);
        }
        else
        {
            FullPath = fileInfo.FileName;
        }
    }

    public string FileName => _fileInfo.FileName;
    public string EncounterId => _fileInfo.EncounterId;
    public string BossName => _fileInfo.BossName;
    public DateTime Timestamp => _fileInfo.Timestamp;
    public long SizeBytes => _fileInfo.SizeBytes;
    public string FormattedSize => _fileInfo.FormattedSize;
    public string FormattedDate => _fileInfo.FormattedDate;
    public string FullPath { get; }

    public string DisplayName => $"{FormattedDate} - {BossName}";
}
