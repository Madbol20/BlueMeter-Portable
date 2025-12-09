using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using BlueMeter.Assets;
using BlueMeter.Core;
using BlueMeter.Core.Data.Models;
using BlueMeter.Core.Data.Database;
using BlueMeter.Core.Analyze.Models;
using BlueMeter.WPF.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace BlueMeter.WPF.ViewModels;

/// <summary>
/// Enhanced Skill Breakdown ViewModel with full StarResonanceDps-style stats
/// Including Lucky Hits tracking!
/// </summary>
public partial class EnhancedSkillBreakdownViewModel : ObservableObject
{
    private readonly ILogger<EnhancedSkillBreakdownViewModel> _logger;
    private readonly IDataStorage _dataStorage;
    private readonly DispatcherTimer _updateTimer;

    [ObservableProperty]
    private List<PlayerSelectionItem> _availablePlayers = new();

    [ObservableProperty]
    private PlayerSelectionItem? _selectedPlayer;

    // Summary Stats
    [ObservableProperty]
    private long _totalDamage;

    [ObservableProperty]
    private long _dps;

    [ObservableProperty]
    private int _totalHits;

    [ObservableProperty]
    private double _critRate;

    [ObservableProperty]
    private int _critCount;

    [ObservableProperty]
    private long _totalCritDamage;

    // Lucky Hits (NEW!)
    [ObservableProperty]
    private double _luckyRate;

    [ObservableProperty]
    private int _luckyCount;

    [ObservableProperty]
    private long _totalLuckyDamage;

    // Distribution
    [ObservableProperty]
    private long _normalDamage;

    [ObservableProperty]
    private long _avgDamage;

    [ObservableProperty]
    private string _duration = "0s";

    // Historical data mode
    [ObservableProperty]
    private bool _isHistoricalDataMode = false;

    private EncounterData? _loadedEncounter;
    private BattleLogFileData[]? _loadedBsonLogs;

    // Enhanced Data (BSON) mode
    [ObservableProperty]
    private bool _useEnhancedData = false;

    [ObservableProperty]
    private string _bsonStatusText = "";

    // Charts
    [ObservableProperty]
    private PlotModel _skillPieChartModel;

    [ObservableProperty]
    private PlotModel _damageDistributionChartModel;

    // Detailed skill table
    [ObservableProperty]
    private ObservableCollection<SkillDetailRow> _skillDetails = new();

    private readonly List<OxyColor> _skillColors = new()
    {
        OxyColor.FromRgb(0, 122, 204),   // Blue
        OxyColor.FromRgb(255, 99, 71),   // Red
        OxyColor.FromRgb(50, 205, 50),   // Green
        OxyColor.FromRgb(255, 165, 0),   // Orange
        OxyColor.FromRgb(147, 112, 219), // Purple
        OxyColor.FromRgb(255, 20, 147),  // Pink
        OxyColor.FromRgb(64, 224, 208),  // Turquoise
        OxyColor.FromRgb(255, 215, 0),   // Gold
        OxyColor.FromRgb(255, 105, 180), // Hot Pink
        OxyColor.FromRgb(0, 191, 255),   // Deep Sky Blue
    };

    public EnhancedSkillBreakdownViewModel(
        ILogger<EnhancedSkillBreakdownViewModel> logger,
        IDataStorage dataStorage,
        Dispatcher dispatcher)
    {
        _logger = logger;
        _dataStorage = dataStorage;

        // Initialize charts
        _skillPieChartModel = CreatePieChartModel();
        _damageDistributionChartModel = CreateBarChartModel();

        // Update timer
        _updateTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };
        _updateTimer.Tick += OnUpdateTick;

        _logger.LogDebug("EnhancedSkillBreakdownViewModel created");
    }

    private PlotModel CreatePieChartModel()
    {
        return new PlotModel
        {
            Title = "Skill Distribution",
            Background = OxyColor.FromRgb(30, 30, 30),
            TextColor = OxyColors.White,
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(63, 63, 70)
        };
    }

    private PlotModel CreateBarChartModel()
    {
        var model = new PlotModel
        {
            Title = "Damage Type Distribution",
            Background = OxyColor.FromRgb(30, 30, 30),
            TextColor = OxyColors.White,
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(63, 63, 70)
        };

        model.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Left,
            Key = "DamageTypes",
            TextColor = OxyColors.White,
            TicklineColor = OxyColors.White
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Damage",
            TextColor = OxyColors.White,
            TitleColor = OxyColors.White,
            TicklineColor = OxyColors.White,
            StringFormat = "N0"
        });

        return model;
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        try
        {
            // Only update in live mode
            if (!IsHistoricalDataMode)
            {
                UpdateAvailablePlayers();
                if (SelectedPlayer != null)
                {
                    UpdateAllStats();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating enhanced skill breakdown");
        }
    }

    private void LoadHistoricalPlayerStats(long playerId)
    {
        _logger.LogInformation("=== LoadHistoricalPlayerStats CALLED for Player {PlayerId} ===", playerId);

        if (_loadedEncounter == null)
        {
            _logger.LogError("_loadedEncounter is NULL!");
            return;
        }

        _logger.LogInformation("_loadedEncounter is valid, checking data source...");

        // Check if BSON data is available
        if (_loadedBsonLogs != null && _loadedBsonLogs.Length > 0)
        {
            _logger.LogInformation("Using BSON data for player {PlayerId}", playerId);
            LoadHistoricalPlayerStatsFromBson(playerId);
            return;
        }

        // Fallback to SQL
        _logger.LogInformation("Using SQL data for player {PlayerId}", playerId);

        if (!_loadedEncounter.PlayerStats.TryGetValue(playerId, out var playerStat))
        {
            _logger.LogWarning("LoadHistoricalPlayerStats: Player {PlayerId} not found in encounter", playerId);
            _logger.LogWarning("Available player IDs: {PlayerIds}",
                string.Join(", ", _loadedEncounter.PlayerStats.Keys));
            return;
        }

        _logger.LogInformation("Found player stats for {PlayerId}, parsing skills...", playerId);

        // Parse skill data from JSON
        Dictionary<long, SkillData> skillDictionary = new();
        if (!string.IsNullOrEmpty(playerStat.SkillDataJson))
        {
            try
            {
                skillDictionary = JsonConvert.DeserializeObject<Dictionary<long, SkillData>>(playerStat.SkillDataJson)
                    ?? new Dictionary<long, SkillData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing skill data JSON for player {PlayerId}", playerId);
            }
        }

        var skills = skillDictionary.Values.ToList();

        // Calculate duration from encounter data
        var durationSeconds = _loadedEncounter.DurationMs / 1000.0;
        Duration = durationSeconds > 0 ? $"{durationSeconds:F1}s" : "0s";

        // Total damage and DPS
        TotalDamage = playerStat.TotalAttackDamage;
        Dps = durationSeconds > 0 ? (long)(playerStat.TotalAttackDamage / durationSeconds) : 0;

        // Aggregate skill stats
        TotalHits = skills.Sum(s => s.UseTimes);
        CritCount = skills.Sum(s => s.CritTimes);
        LuckyCount = skills.Sum(s => s.LuckyTimes);

        // Calculate rates
        CritRate = TotalHits > 0 ? (double)CritCount / TotalHits : 0;
        LuckyRate = TotalHits > 0 ? (double)LuckyCount / TotalHits : 0;

        // Calculate damage breakdown
        TotalCritDamage = (long)(TotalDamage * CritRate);
        TotalLuckyDamage = (long)(TotalDamage * LuckyRate * 0.5);
        NormalDamage = TotalDamage - TotalCritDamage - TotalLuckyDamage;
        AvgDamage = TotalHits > 0 ? TotalDamage / TotalHits : 0;

        // Update charts
        UpdateSkillPieChart(skills, durationSeconds);
        UpdateDamageDistributionChart();

        // Update skill details table
        UpdateSkillDetailsTable(skills, durationSeconds);

        _logger.LogDebug("LoadHistoricalPlayerStats: Loaded stats for player {PlayerId}, TotalDamage={Damage}, DPS={Dps}",
            playerId, TotalDamage, Dps);
    }

    /// <summary>
    /// Load player stats by aggregating BSON BattleLog events
    /// </summary>
    private void LoadHistoricalPlayerStatsFromBson(long playerId)
    {
        _logger.LogInformation("=== LoadHistoricalPlayerStatsFromBson for Player {PlayerId} ===", playerId);

        if (_loadedBsonLogs == null || _loadedEncounter == null)
        {
            _logger.LogError("No BSON data or encounter data available");
            return;
        }

        // Filter logs for this player (damage only, no heals, no misses)
        var playerLogs = _loadedBsonLogs
            .Where(log => log.AttackerUuid == playerId
                && log.IsAttackerPlayer
                && !log.IsHeal
                && !log.IsMiss
                && log.Value > 0)
            .ToList();

        _logger.LogInformation("Found {Count} damage events for player {PlayerId}", playerLogs.Count, playerId);

        // Aggregate into SkillData dictionary
        var skillDictionary = new Dictionary<long, SkillData>();

        foreach (var log in playerLogs)
        {
            if (!skillDictionary.TryGetValue(log.SkillID, out var skillData))
            {
                // Create new skill entry
                skillDictionary[log.SkillID] = new SkillData
                {
                    SkillId = log.SkillID,
                    TotalValue = log.Value,
                    UseTimes = 1,
                    CritTimes = log.IsCritical ? 1 : 0,
                    LuckyTimes = log.IsLucky ? 1 : 0,
                    MinDamage = log.Value,
                    MaxDamage = log.Value,
                    HighestCrit = log.IsCritical ? log.Value : 0
                };
            }
            else
            {
                // Update existing skill entry (recreate object)
                skillDictionary[log.SkillID] = new SkillData
                {
                    SkillId = log.SkillID,
                    TotalValue = skillData.TotalValue + log.Value,
                    UseTimes = skillData.UseTimes + 1,
                    CritTimes = skillData.CritTimes + (log.IsCritical ? 1 : 0),
                    LuckyTimes = skillData.LuckyTimes + (log.IsLucky ? 1 : 0),
                    MinDamage = Math.Min(skillData.MinDamage, log.Value),
                    MaxDamage = Math.Max(skillData.MaxDamage, log.Value),
                    HighestCrit = log.IsCritical ? Math.Max(skillData.HighestCrit, log.Value) : skillData.HighestCrit
                };
            }
        }

        var skills = skillDictionary.Values.ToList();

        _logger.LogInformation("Aggregated into {Count} skills", skills.Count);

        // Calculate duration from encounter data
        var durationSeconds = _loadedEncounter.DurationMs / 1000.0;
        Duration = durationSeconds > 0 ? $"{durationSeconds:F1}s" : "0s";

        // Total damage and DPS
        TotalDamage = skills.Sum(s => s.TotalValue);
        Dps = durationSeconds > 0 ? (long)(TotalDamage / durationSeconds) : 0;

        // Aggregate skill stats
        TotalHits = skills.Sum(s => s.UseTimes);
        CritCount = skills.Sum(s => s.CritTimes);
        LuckyCount = skills.Sum(s => s.LuckyTimes);

        // Calculate rates
        CritRate = TotalHits > 0 ? (double)CritCount / TotalHits : 0;
        LuckyRate = TotalHits > 0 ? (double)LuckyCount / TotalHits : 0;

        // Calculate damage breakdown
        TotalCritDamage = (long)(TotalDamage * CritRate);
        TotalLuckyDamage = (long)(TotalDamage * LuckyRate * 0.5);
        NormalDamage = TotalDamage - TotalCritDamage - TotalLuckyDamage;
        AvgDamage = TotalHits > 0 ? TotalDamage / TotalHits : 0;

        // Update charts
        UpdateSkillPieChart(skills, durationSeconds);
        UpdateDamageDistributionChart();

        // Update skill details table
        UpdateSkillDetailsTable(skills, durationSeconds);

        _logger.LogInformation("LoadHistoricalPlayerStatsFromBson COMPLETE: TotalDamage={Damage}, DPS={Dps}, Skills={SkillCount}",
            TotalDamage, Dps, skills.Count);
    }

    private void UpdateAvailablePlayers()
    {
        var currentPlayerIds = _dataStorage.ReadOnlyPlayerInfoDatas.Keys.ToList();
        var dpsPlayerIds = _dataStorage.ReadOnlyFullDpsDatas.Keys.ToList();
        var activePlayerIds = currentPlayerIds.Intersect(dpsPlayerIds).ToList();

        var currentAvailableIds = AvailablePlayers.Select(p => p.PlayerId).ToList();
        if (activePlayerIds.SequenceEqual(currentAvailableIds))
        {
            return;
        }

        var newPlayers = new List<PlayerSelectionItem>();
        foreach (var playerId in activePlayerIds)
        {
            if (_dataStorage.ReadOnlyPlayerInfoDatas.TryGetValue(playerId, out var playerInfo))
            {
                // FILTER: Only include actual players (with ProfessionID and valid names)
                // Enemies/NPCs don't have ProfessionID
                if (playerInfo.ProfessionID.HasValue && playerInfo.ProfessionID.Value > 0)
                {
                    newPlayers.Add(new PlayerSelectionItem
                    {
                        PlayerId = playerId,
                        PlayerName = playerInfo.Name ?? $"Player {playerId}"
                    });
                }
            }
        }

        AvailablePlayers = newPlayers.OrderBy(p => p.PlayerName).ToList();
    }

    private void UpdateAllStats()
    {
        if (SelectedPlayer == null)
        {
            _logger.LogDebug("UpdateAllStats: No player selected");
            return;
        }

        // Check if we're in historical mode
        if (IsHistoricalDataMode && _loadedEncounter != null)
        {
            LoadHistoricalPlayerStats(SelectedPlayer.PlayerId);
            return;
        }

        // Live mode: Try Full first, then Sectioned
        if (!_dataStorage.ReadOnlyFullDpsDatas.TryGetValue(SelectedPlayer.PlayerId, out var dpsData))
        {
            if (!_dataStorage.ReadOnlySectionedDpsDataList.Any(d => d.UID == SelectedPlayer.PlayerId))
            {
                _logger.LogWarning("UpdateAllStats: No data found for player {PlayerId}", SelectedPlayer.PlayerId);
                return;
            }
            dpsData = _dataStorage.ReadOnlySectionedDpsDataList.First(d => d.UID == SelectedPlayer.PlayerId);
        }

        _logger.LogDebug("UpdateAllStats: Found data for player {PlayerId}, TotalDamage={Damage}",
            SelectedPlayer.PlayerId, dpsData.TotalAttackDamage);

        // Calculate duration from ticks (same as BlueMeter's existing logic)
        var durationTicks = (dpsData.LastLoggedTick - (dpsData.StartLoggedTick ?? 0));
        var durationSeconds = durationTicks / 10000000.0; // Ticks to seconds
        Duration = durationSeconds > 0 ? $"{durationSeconds:F1}s" : "0s";

        // Total damage and DPS
        TotalDamage = dpsData.TotalAttackDamage;
        Dps = durationSeconds > 0 ? (long)(dpsData.TotalAttackDamage / durationSeconds) : 0;

        _logger.LogDebug("UpdateAllStats: Duration={Duration}s, DPS={DPS}", durationSeconds, Dps);

        // Aggregate skill stats
        var skills = dpsData.ReadOnlySkillDatas.Values.ToList();

        TotalHits = skills.Sum(s => s.UseTimes);
        CritCount = skills.Sum(s => s.CritTimes);
        LuckyCount = skills.Sum(s => s.LuckyTimes);

        // Calculate rates
        CritRate = TotalHits > 0 ? (double)CritCount / TotalHits : 0;
        LuckyRate = TotalHits > 0 ? (double)LuckyCount / TotalHits : 0;

        // Calculate damage breakdown
        // Note: We don't have separate damage values, so we estimate based on hit types
        // In a real implementation, you'd track this separately
        TotalCritDamage = (long)(TotalDamage * CritRate);
        TotalLuckyDamage = (long)(TotalDamage * LuckyRate * 0.5); // Approximate lucky damage contribution
        NormalDamage = TotalDamage - TotalCritDamage - TotalLuckyDamage;
        AvgDamage = TotalHits > 0 ? TotalDamage / TotalHits : 0;

        // Update charts
        UpdateSkillPieChart(skills, durationSeconds);
        UpdateDamageDistributionChart();

        // Update skill details table
        UpdateSkillDetailsTable(skills, durationSeconds);
    }

    private void UpdateSkillPieChart(List<SkillData> skills, double duration)
    {
        var topSkills = skills
            .OrderByDescending(s => s.TotalValue)
            .Take(10)
            .ToList();

        var pieSeries = new PieSeries
        {
            StrokeThickness = 2,
            Stroke = OxyColor.FromRgb(30, 30, 30),
            InsideLabelColor = OxyColors.White,
            InsideLabelPosition = 0.5,
            TextColor = OxyColors.White,
            FontSize = 11,
            OutsideLabelFormat = "{0}: {2:P0}"
        };

        for (int i = 0; i < topSkills.Count; i++)
        {
            var skill = topSkills[i];
            var skillName = GetSkillName(skill.SkillId);
            pieSeries.Slices.Add(new PieSlice(skillName, skill.TotalValue)
            {
                Fill = _skillColors[i % _skillColors.Count]
            });
        }

        SkillPieChartModel.Series.Clear();
        SkillPieChartModel.Series.Add(pieSeries);
        SkillPieChartModel.InvalidatePlot(true);
    }

    private void UpdateDamageDistributionChart()
    {
        var barSeries = new BarSeries
        {
            FillColor = OxyColor.FromRgb(0, 122, 204),
            StrokeThickness = 0
        };

        barSeries.Items.Add(new BarItem { Value = NormalDamage });
        barSeries.Items.Add(new BarItem { Value = TotalCritDamage, Color = OxyColor.FromRgb(255, 99, 71) });
        barSeries.Items.Add(new BarItem { Value = TotalLuckyDamage, Color = OxyColor.FromRgb(255, 215, 0) });

        var categoryAxis = DamageDistributionChartModel.Axes.FirstOrDefault(a => a.Key == "DamageTypes") as CategoryAxis;
        if (categoryAxis != null)
        {
            categoryAxis.Labels.Clear();
            categoryAxis.Labels.Add("Normal");
            categoryAxis.Labels.Add("Critical");
            categoryAxis.Labels.Add("Lucky");
        }

        DamageDistributionChartModel.Series.Clear();
        DamageDistributionChartModel.Series.Add(barSeries);
        DamageDistributionChartModel.InvalidatePlot(true);
    }

    private void UpdateSkillDetailsTable(List<SkillData> skills, double duration)
    {
        var details = skills
            .OrderByDescending(s => s.TotalValue)
            .Select(skill =>
            {
                var hits = skill.UseTimes;
                var critRate = hits > 0 ? (double)skill.CritTimes / hits : 0;
                var luckyRate = hits > 0 ? (double)skill.LuckyTimes / hits : 0;
                var dps = duration > 0 ? (long)(skill.TotalValue / duration) : 0;
                var avgPerHit = hits > 0 ? skill.TotalValue / hits : 0;
                var percentage = TotalDamage > 0 ? (double)skill.TotalValue / TotalDamage : 0;

                return new SkillDetailRow
                {
                    SkillName = GetSkillName(skill.SkillId),
                    TotalDamage = skill.TotalValue,
                    DPS = dps,
                    HitCount = hits,
                    CritRate = critRate,
                    CritCount = skill.CritTimes,
                    LuckyRate = luckyRate,
                    LuckyCount = skill.LuckyTimes,
                    AvgPerHit = avgPerHit,
                    Percentage = percentage
                };
            })
            .ToList();

        SkillDetails.Clear();
        foreach (var detail in details)
        {
            SkillDetails.Add(detail);
        }
    }

    private string GetSkillName(long skillId)
    {
        var skillIdText = skillId.ToString();
        var skillName = EmbeddedSkillConfig.TryGet(skillIdText, out var definition)
            ? definition.Name
            : skillIdText;

        // Translate if available
        var translatedSkillName = DpsStatisticsSubViewModel.GetTranslator()?.Translate(skillName) ?? skillName;
        return translatedSkillName;
    }

    partial void OnSelectedPlayerChanged(PlayerSelectionItem? value)
    {
        if (value != null)
        {
            _logger.LogInformation("Selected player changed: {PlayerName}", value.PlayerName);
            UpdateAllStats();
        }
    }

    public void OnViewLoaded()
    {
        UpdateAvailablePlayers();

        // Auto-select first player if available
        if (AvailablePlayers.Count > 0 && SelectedPlayer == null)
        {
            SelectedPlayer = AvailablePlayers.First();
            _logger.LogInformation("EnhancedSkillBreakdown: Auto-selected first player: {Name}", SelectedPlayer.PlayerName);
        }

        _updateTimer.Start();
        _logger.LogDebug("EnhancedSkillBreakdownViewModel view loaded, timer started. Players available: {Count}", AvailablePlayers.Count);
    }

    public void OnViewUnloaded()
    {
        _updateTimer.Stop();
        _logger.LogDebug("EnhancedSkillBreakdownViewModel view unloaded, timer stopped");
    }

    public void SetFocusedPlayer(long? playerId)
    {
        if (!playerId.HasValue) return;

        UpdateAvailablePlayers();
        var player = AvailablePlayers.FirstOrDefault(p => p.PlayerId == playerId.Value);
        if (player != null)
        {
            SelectedPlayer = player;
        }
    }

    /// <summary>
    /// Load historical encounter data
    /// </summary>
    public void LoadHistoricalEncounter(EncounterData encounterData)
    {
        _logger.LogInformation("=== LoadHistoricalEncounter CALLED (BSON-first mode) ===");
        _logger.LogInformation("Encounter ID: {EncounterId}, Start: {StartTime}, Duration: {Duration}ms",
            encounterData.EncounterId, encounterData.StartTime, encounterData.DurationMs);

        _loadedEncounter = encounterData;
        IsHistoricalDataMode = true;

        // Stop auto-updates
        _updateTimer.Stop();
        _logger.LogInformation("Auto-update timer stopped");

        // Historical data: Try BSON first, fallback to SQL
        bool loadedFromBson = TryLoadFromBson(encounterData);

        if (!loadedFromBson)
        {
            // Fallback to SQL
            _logger.LogInformation("BSON not available, using SQL fallback");
            LoadFromSql(encounterData);
            BsonStatusText = "📊 SQL (no BSON)";
        }
        else
        {
            BsonStatusText = "✓ BSON data";
        }

        _logger.LogInformation("=== LoadHistoricalEncounter COMPLETE ===");
    }

    /// <summary>
    /// Try to load from BSON file (returns true if successful)
    /// </summary>
    private bool TryLoadFromBson(EncounterData encounterData)
    {
        try
        {
            var battleLogManager = Core.Data.DataStorageExtensions.GetBattleLogManager();
            if (battleLogManager == null)
            {
                _logger.LogInformation("BattleLogManager not available");
                return false;
            }

            // Find BSON file (match by first 8 chars of EncounterId)
            var storedEncounters = battleLogManager.GetStoredEncounters();
            var encounterIdPrefix = encounterData.EncounterId.Length > 8
                ? encounterData.EncounterId.Substring(0, 8)
                : encounterData.EncounterId;

            var bsonEncounter = storedEncounters.FirstOrDefault(e =>
                e.EncounterId == encounterIdPrefix ||
                encounterData.EncounterId.StartsWith(e.EncounterId));

            if (bsonEncounter == null)
            {
                _logger.LogInformation("No BSON file found for encounter {EncounterId}", encounterData.EncounterId);
                return false;
            }

            _logger.LogInformation("Found BSON file: {FileName}", bsonEncounter.FileName);

            // Load BSON file
            var filePath = System.IO.Path.Combine(battleLogManager.LogDirectory, bsonEncounter.FileName);
            var logsFile = Core.Analyze.BattleLogReader.ReadFile(filePath);

            if (logsFile == null || logsFile.BattleLogs == null || logsFile.BattleLogs.Length == 0)
            {
                _logger.LogWarning("BSON file is empty");
                return false;
            }

            _logger.LogInformation("Loaded {Count} battle log events from BSON", logsFile.BattleLogs.Length);

            // Store BSON logs for later aggregation
            _loadedBsonLogs = logsFile.BattleLogs;

            // Extract REAL players from BSON PlayerInfos (with ProfessionID)
            var realPlayerIds = new HashSet<long>();
            if (logsFile.PlayerInfos != null && logsFile.PlayerInfos.Length > 0)
            {
                foreach (var playerInfo in logsFile.PlayerInfos)
                {
                    // FILTER: Only actual players with ProfessionID (not enemies/NPCs)
                    if (playerInfo.ProfessionID.HasValue && playerInfo.ProfessionID.Value > 0)
                    {
                        realPlayerIds.Add(playerInfo.UID);
                    }
                }
                _logger.LogInformation("Found {Count} real players in BSON PlayerInfos", realPlayerIds.Count);
            }
            else
            {
                _logger.LogWarning("No PlayerInfos in BSON file, using fallback filter");
                // Fallback: use attackers who dealt damage AND are in encounterData.PlayerStats with profession
                var attackerIds = logsFile.BattleLogs
                    .Where(log => log.IsAttackerPlayer && !log.IsHeal && !log.IsMiss && log.Value > 0)
                    .Select(log => log.AttackerUuid)
                    .Distinct()
                    .ToList();

                foreach (var attackerId in attackerIds)
                {
                    if (encounterData.PlayerStats.TryGetValue(attackerId, out var stats))
                    {
                        // TODO: EncounterData doesn't have ProfessionID, so accept all for now
                        realPlayerIds.Add(attackerId);
                    }
                }
            }

            // Build player list
            var newPlayers = new List<PlayerSelectionItem>();
            foreach (var playerId in realPlayerIds)
            {
                // Get player name from BSON PlayerInfos or EncounterData
                string? playerName = null;

                if (logsFile.PlayerInfos != null)
                {
                    var playerInfo = logsFile.PlayerInfos.FirstOrDefault(p => p.UID == playerId);
                    playerName = playerInfo?.Name;
                }

                if (string.IsNullOrEmpty(playerName) && encounterData.PlayerStats.TryGetValue(playerId, out var stats))
                {
                    playerName = stats.Name;
                }

                playerName ??= $"Player {playerId}";

                newPlayers.Add(new PlayerSelectionItem
                {
                    PlayerId = playerId,
                    PlayerName = playerName
                });
            }

            AvailablePlayers = newPlayers.OrderBy(p => p.PlayerName).ToList();
            _logger.LogInformation("Loaded {Count} players from BSON", AvailablePlayers.Count);

            // Auto-select first player
            if (AvailablePlayers.Count > 0)
            {
                SelectedPlayer = AvailablePlayers.First();
                _logger.LogInformation("Auto-selected player: {PlayerName}", SelectedPlayer.PlayerName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading from BSON");
            return false;
        }
    }

    /// <summary>
    /// Fallback: Load from SQL
    /// </summary>
    private void LoadFromSql(EncounterData encounterData)
    {
        var newPlayers = new List<PlayerSelectionItem>();
        foreach (var kvp in encounterData.PlayerStats)
        {
            newPlayers.Add(new PlayerSelectionItem
            {
                PlayerId = kvp.Key,
                PlayerName = kvp.Value.Name ?? $"Player {kvp.Key}"
            });
        }

        AvailablePlayers = newPlayers.OrderBy(p => p.PlayerName).ToList();
        _logger.LogInformation("Loaded {Count} players from SQL", AvailablePlayers.Count);

        // Auto-select first player
        if (AvailablePlayers.Count > 0)
        {
            SelectedPlayer = AvailablePlayers.First();
            _logger.LogInformation("Auto-selected player: {PlayerName}", SelectedPlayer.PlayerName);
            UpdateAllStats();
        }
        else
        {
            _logger.LogWarning("No players available!");
        }
    }

    /// <summary>
    /// Restore live data mode
    /// </summary>
    public void RestoreLiveData()
    {
        _logger.LogInformation("Restoring live data mode for Enhanced Skill Breakdown");

        _loadedEncounter = null;
        _loadedBsonLogs = null;
        IsHistoricalDataMode = false;
        UseEnhancedData = false;

        // Restart auto-updates
        _updateTimer.Start();

        // Update player list from live data
        UpdateAvailablePlayers();

        // Auto-select first player if available
        if (AvailablePlayers.Count > 0 && SelectedPlayer == null)
        {
            SelectedPlayer = AvailablePlayers.First();
        }

        // Update stats
        if (SelectedPlayer != null)
        {
            UpdateAllStats();
        }

        _logger.LogInformation("Live data mode restored");
    }

    /// <summary>
    /// Handle UseEnhancedData toggle changed
    /// </summary>
    partial void OnUseEnhancedDataChanged(bool value)
    {
        _logger.LogInformation("UseEnhancedData changed to: {Value}", value);

        if (value)
        {
            // Try to load from BSON
            LoadFromBsonFile();
        }
        else
        {
            // Reload from SQL database
            if (IsHistoricalDataMode && _loadedEncounter != null && SelectedPlayer != null)
            {
                LoadHistoricalPlayerStats(SelectedPlayer.PlayerId);
            }
            else if (SelectedPlayer != null)
            {
                UpdateAllStats();
            }
        }
    }

    /// <summary>
    /// Load skill data from BSON file
    /// </summary>
    private void LoadFromBsonFile()
    {
        try
        {
            if (_loadedEncounter == null || SelectedPlayer == null)
            {
                BsonStatusText = "❌ No encounter selected";
                _logger.LogWarning("Cannot load BSON: No encounter or player selected");
                return;
            }

            _logger.LogInformation("Loading BSON data for encounter {EncounterId}", _loadedEncounter.EncounterId);

            // Get BattleLogManager
            var battleLogManager = Core.Data.DataStorageExtensions.GetBattleLogManager();
            if (battleLogManager == null)
            {
                BsonStatusText = "❌ Advanced Logging not enabled";
                _logger.LogWarning("BattleLogManager is null - Advanced Logging not enabled");
                UseEnhancedData = false;
                return;
            }

            // Find BSON file for this encounter
            // Note: BSON filenames use truncated EncounterId (first 8 chars), so we need to match by prefix
            var storedEncounters = battleLogManager.GetStoredEncounters();
            var encounterIdPrefix = _loadedEncounter.EncounterId.Length > 8
                ? _loadedEncounter.EncounterId.Substring(0, 8)
                : _loadedEncounter.EncounterId;

            var bsonEncounter = storedEncounters.FirstOrDefault(e =>
                e.EncounterId == encounterIdPrefix ||
                _loadedEncounter.EncounterId.StartsWith(e.EncounterId));

            if (bsonEncounter == null)
            {
                BsonStatusText = $"❌ No BSON file for this encounter";
                _logger.LogWarning("No BSON file found for encounter {EncounterId} (prefix: {Prefix})",
                    _loadedEncounter.EncounterId, encounterIdPrefix);
                _logger.LogInformation("Available BSON encounters: {Encounters}",
                    string.Join(", ", storedEncounters.Select(e => e.EncounterId)));
                UseEnhancedData = false;
                return;
            }

            _logger.LogInformation("Found BSON file: {FileName}", bsonEncounter.FileName);

            // Load BSON data using BattleLogReader
            var filePath = System.IO.Path.Combine(battleLogManager.LogDirectory, bsonEncounter.FileName);
            var encounterLog = Core.Analyze.BattleLogReader.ReadFile(filePath);

            if (encounterLog == null || encounterLog.BattleLogs == null || encounterLog.BattleLogs.Length == 0)
            {
                BsonStatusText = "❌ Failed to load BSON data";
                _logger.LogError("Failed to load BSON data from {FilePath}", filePath);
                UseEnhancedData = false;
                return;
            }

            _logger.LogInformation("Loaded {Count} battle log events from BSON", encounterLog.BattleLogs.Length);

            // Convert BattleLogs to skill stats - filter for damage events from this player
            var playerLogs = encounterLog.BattleLogs
                .Where(log => log.AttackerUuid == SelectedPlayer.PlayerId && !log.IsHeal && !log.IsMiss)
                .ToList();

            _logger.LogInformation("Found {Count} damage events for player {PlayerId}", playerLogs.Count, SelectedPlayer.PlayerId);

            if (playerLogs.Count == 0)
            {
                BsonStatusText = "❌ No data for selected player";
                UseEnhancedData = false;
                return;
            }

            // Aggregate skills from battle logs using same logic as DataStorage.cs
            var skillDictionary = new Dictionary<long, SkillData>();
            foreach (var log in playerLogs)
            {
                if (log.SkillID == 0) continue;

                if (!skillDictionary.TryGetValue(log.SkillID, out var skillData))
                {
                    skillData = new SkillData
                    {
                        SkillId = log.SkillID,
                        MinDamage = long.MaxValue
                    };
                    skillDictionary[log.SkillID] = skillData;
                }

                // Aggregate using same logic as DataStorage.UpdateSkillData
                skillData.TotalValue += log.Value;
                skillData.UseTimes += 1;
                skillData.CritTimes += log.IsCritical ? 1 : 0;
                skillData.LuckyTimes += log.IsLucky ? 1 : 0;

                // Track min/max damage
                if (log.Value > 0)
                {
                    skillData.MinDamage = Math.Min(skillData.MinDamage, log.Value);
                    skillData.MaxDamage = Math.Max(skillData.MaxDamage, log.Value);

                    if (log.IsCritical)
                    {
                        skillData.HighestCrit = Math.Max(skillData.HighestCrit, log.Value);
                    }
                }
            }

            var skills = skillDictionary.Values.ToList();

            // Calculate duration
            var durationSeconds = _loadedEncounter.DurationMs / 1000.0;
            Duration = durationSeconds > 0 ? $"{durationSeconds:F1}s" : "0s";

            // Update stats
            TotalDamage = skills.Sum(s => s.TotalValue);
            Dps = durationSeconds > 0 ? (long)(TotalDamage / durationSeconds) : 0;
            TotalHits = skills.Sum(s => s.UseTimes);
            CritCount = skills.Sum(s => s.CritTimes);
            LuckyCount = skills.Sum(s => s.LuckyTimes);

            CritRate = TotalHits > 0 ? (double)CritCount / TotalHits : 0;
            LuckyRate = TotalHits > 0 ? (double)LuckyCount / TotalHits : 0;

            TotalCritDamage = (long)(TotalDamage * CritRate);
            TotalLuckyDamage = (long)(TotalDamage * LuckyRate * 0.5);
            NormalDamage = TotalDamage - TotalCritDamage - TotalLuckyDamage;
            AvgDamage = TotalHits > 0 ? TotalDamage / TotalHits : 0;

            // Update charts
            UpdateSkillPieChart(skills, durationSeconds);
            UpdateDamageDistributionChart();
            UpdateSkillDetailsTable(skills, durationSeconds);

            BsonStatusText = $"✓ Loaded {encounterLog.BattleLogs.Length} events ({skills.Count} skills)";
            _logger.LogInformation("Successfully loaded BSON data: {TotalDamage} damage, {HitCount} hits, {SkillCount} skills",
                TotalDamage, TotalHits, skills.Count);
        }
        catch (Exception ex)
        {
            BsonStatusText = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Error loading BSON data");
            UseEnhancedData = false;
        }
    }
}

/// <summary>
/// Row data for the detailed skill breakdown table
/// </summary>
public class SkillDetailRow
{
    public string SkillName { get; init; } = "";
    public long TotalDamage { get; init; }
    public long DPS { get; init; }
    public int HitCount { get; init; }
    public double CritRate { get; init; }
    public int CritCount { get; init; }
    public double LuckyRate { get; init; }
    public int LuckyCount { get; init; }
    public long AvgPerHit { get; init; }
    public double Percentage { get; init; }
}
