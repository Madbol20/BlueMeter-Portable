using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BlueMeter.WPF.Services.ModuleSolver;
using BlueMeter.WPF.Services.ModuleSolver.Models;

namespace BlueMeter.WPF.ViewModels;

public partial class ModuleSolveViewModel : BaseViewModel, IDisposable
{
    private readonly PacketCaptureService _packetCaptureService;
    private readonly ModuleOptimizerService _optimizerService;
    private readonly ModulePersistenceService _persistenceService;
    private readonly ILogger<ModuleSolveViewModel> _logger;
    private CancellationTokenSource? _captureCts;

    [ObservableProperty]
    private ObservableCollection<string> _availableAttributes = new();

    [ObservableProperty]
    private string? _selectedTargetAttribute1;

    [ObservableProperty]
    private string? _selectedTargetAttribute2;

    [ObservableProperty]
    private string? _selectedTargetAttribute3;

    [ObservableProperty]
    private string? _selectedTargetAttribute4;

    [ObservableProperty]
    private string? _selectedTargetAttribute5;

    [ObservableProperty]
    private string? _selectedExcludeAttribute1;

    [ObservableProperty]
    private string? _selectedExcludeAttribute2;

    [ObservableProperty]
    private string? _selectedExcludeAttribute3;

    [ObservableProperty]
    private string? _selectedExcludeAttribute4;

    [ObservableProperty]
    private string? _selectedExcludeAttribute5;

    [ObservableProperty]
    private int _moduleTypeIndex = 0; // 0 = All, 1 = Attack, 2 = Defense, 3 = Support

    [ObservableProperty]
    private int _sortTypeIndex = 0; // 0 = Attribute (weighted), 1 = Level (total value)

    [ObservableProperty]
    private bool _isCapturing = false;

    [ObservableProperty]
    private bool _hasModules = false;

    [ObservableProperty]
    private string _statusMessage = "Ready to capture module data";

    [ObservableProperty]
    private ObservableCollection<ModuleSolution> _solutions = new();

    [ObservableProperty]
    private ModuleSolution? _selectedSolution;

    private List<ModuleInfo> _capturedModules = new();

    public ModuleSolveViewModel(
        PacketCaptureService packetCaptureService,
        ModuleOptimizerService optimizerService,
        ModulePersistenceService persistenceService,
        ILogger<ModuleSolveViewModel> logger)
    {
        _packetCaptureService = packetCaptureService;
        _optimizerService = optimizerService;
        _persistenceService = persistenceService;
        _logger = logger;

        // Subscribe to packet capture events
        _packetCaptureService.ModulesCapture += OnModulesCaptured;

        // Initialize available attributes
        var attributes = ModuleConstants.GetAllAttributeNames();
        attributes.Insert(0, ""); // Add empty option
        AvailableAttributes = new ObservableCollection<string>(attributes);
    }

    [RelayCommand]
    private async Task StartCaptureAsync()
    {
        try
        {
            IsCapturing = true;
            StatusMessage = "Starting packet capture... Open your game inventory to capture module data.";

            _captureCts = new CancellationTokenSource();

            // Get available network devices
            var devices = _packetCaptureService.GetNetworkDevices();

            if (devices.Count == 0)
            {
                StatusMessage = "No network devices found. Please check your network adapter settings.";
                _logger.LogError("No network devices available for capture");
                IsCapturing = false;
                return;
            }

            // Use the first device by default (can be enhanced with device selection UI)
            _logger.LogInformation("Using network device: {Device}", devices[0].Description);
            StatusMessage = $"Listening on: {devices[0].Description}";

            await _packetCaptureService.StartCaptureAsync(0, _captureCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting packet capture");
            StatusMessage = $"Error: {ex.Message}";
            IsCapturing = false;
        }
    }

    [RelayCommand]
    private void StopCapture()
    {
        _captureCts?.Cancel();
        _packetCaptureService.StopCapture();
        IsCapturing = false;
        StatusMessage = HasModules
            ? $"Capture stopped. {_capturedModules.Count} modules captured."
            : "Capture stopped. No modules captured yet.";
    }

    [RelayCommand]
    private void AnalyzeModules()
    {
        if (!HasModules)
        {
            StatusMessage = "Please capture module data first.";
            return;
        }

        try
        {
            StatusMessage = "Analyzing modules...";

            // Build target attributes list
            var targetAttributes = new List<string>();
            if (!string.IsNullOrEmpty(SelectedTargetAttribute1)) targetAttributes.Add(SelectedTargetAttribute1);
            if (!string.IsNullOrEmpty(SelectedTargetAttribute2)) targetAttributes.Add(SelectedTargetAttribute2);
            if (!string.IsNullOrEmpty(SelectedTargetAttribute3)) targetAttributes.Add(SelectedTargetAttribute3);
            if (!string.IsNullOrEmpty(SelectedTargetAttribute4)) targetAttributes.Add(SelectedTargetAttribute4);
            if (!string.IsNullOrEmpty(SelectedTargetAttribute5)) targetAttributes.Add(SelectedTargetAttribute5);

            // Build exclude attributes list
            var excludeAttributes = new List<string>();
            if (!string.IsNullOrEmpty(SelectedExcludeAttribute1)) excludeAttributes.Add(SelectedExcludeAttribute1);
            if (!string.IsNullOrEmpty(SelectedExcludeAttribute2)) excludeAttributes.Add(SelectedExcludeAttribute2);
            if (!string.IsNullOrEmpty(SelectedExcludeAttribute3)) excludeAttributes.Add(SelectedExcludeAttribute3);
            if (!string.IsNullOrEmpty(SelectedExcludeAttribute4)) excludeAttributes.Add(SelectedExcludeAttribute4);
            if (!string.IsNullOrEmpty(SelectedExcludeAttribute5)) excludeAttributes.Add(SelectedExcludeAttribute5);

            // Determine module category
            var category = ModuleTypeIndex switch
            {
                1 => ModuleCategory.Attack,
                2 => ModuleCategory.Defense,
                3 => ModuleCategory.Support,
                _ => ModuleCategory.All
            };

            // Determine sort type
            bool sortByLevel = SortTypeIndex == 1;

            _logger.LogInformation("Running optimization with {TargetCount} target attributes, {ExcludeCount} exclude attributes",
                targetAttributes.Count, excludeAttributes.Count);

            // Run optimization
            var solutions = _optimizerService.OptimizeModules(
                _capturedModules,
                targetAttributes.Count > 0 ? targetAttributes : null,
                excludeAttributes.Count > 0 ? excludeAttributes : null,
                category,
                sortByLevel);

            Solutions = new ObservableCollection<ModuleSolution>(solutions);
            SelectedSolution = Solutions.FirstOrDefault();

            StatusMessage = $"Analysis complete. Found {solutions.Count} optimal combinations.";
            _logger.LogInformation("Analysis complete: {Count} solutions found", solutions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing modules");
            StatusMessage = $"Analysis error: {ex.Message}";
        }
    }

    private void OnModulesCaptured(object? sender, List<ModuleInfo> modules)
    {
        _capturedModules = modules;
        HasModules = modules.Count > 0;

        StatusMessage = $"Successfully captured {modules.Count} modules! Click 'Analyze' to find optimal combinations.";
        _logger.LogInformation("Captured {Count} modules from game data", modules.Count);

        // Auto-save captured modules
        _ = AutoSaveModulesAsync(modules);

        // Automatically trigger analysis with current settings
        AnalyzeModules();
    }

    private async Task AutoSaveModulesAsync(List<ModuleInfo> modules)
    {
        try
        {
            var filePath = await _persistenceService.SaveModulesAsync(modules);
            _logger.LogInformation("Auto-saved modules to: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-save modules");
        }
    }

    [RelayCommand]
    private async Task SaveModulesAsync()
    {
        if (!HasModules)
        {
            StatusMessage = "No modules to save. Capture module data first.";
            return;
        }

        try
        {
            var filePath = await _persistenceService.SaveModulesAsync(_capturedModules);
            StatusMessage = $"Saved {_capturedModules.Count} modules to: {Path.GetFileName(filePath)}";
            _logger.LogInformation("Manually saved modules to: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving modules");
            StatusMessage = $"Error saving modules: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadModulesAsync()
    {
        try
        {
            // Get most recent file
            var savedFiles = _persistenceService.GetSavedFiles();
            if (savedFiles.Count == 0)
            {
                StatusMessage = "No saved module files found.";
                return;
            }

            // Load the most recent file (files are sorted by name which includes timestamp)
            savedFiles.Sort();
            var latestFile = savedFiles[^1];

            var modules = await _persistenceService.LoadModulesAsync(latestFile);
            _capturedModules = modules;
            HasModules = modules.Count > 0;

            StatusMessage = $"Loaded {modules.Count} modules from: {latestFile}";
            _logger.LogInformation("Loaded modules from: {File}", latestFile);

            // Automatically trigger analysis
            AnalyzeModules();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading modules");
            StatusMessage = $"Error loading modules: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            var dataDir = _persistenceService.GetDataDirectory();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dataDir,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening data folder");
            StatusMessage = $"Error opening folder: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _packetCaptureService.ModulesCapture -= OnModulesCaptured;
        _captureCts?.Cancel();
        _captureCts?.Dispose();
        _packetCaptureService.Dispose();
    }
}
