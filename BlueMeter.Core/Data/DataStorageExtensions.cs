using System;
using System.Linq;
using System.Threading.Tasks;
using BlueMeter.Core.Data.Database;
using BlueMeter.Core.Data.Models;
using BlueMeter.WPF.Data;

namespace BlueMeter.Core.Data;

/// <summary>
/// Extension methods for integrating DataStorage with database persistence
/// </summary>
public static class DataStorageExtensions
{
    private static EncounterService? _encounterService;
    private static IDataStorage? _dataStorage;
    private static object? _chartDataService; // IChartDataService from WPF
    private static bool _isInitialized;
    private static DateTime _lastSaveTime = DateTime.MinValue;
    private static readonly TimeSpan MinSaveDuration = TimeSpan.FromSeconds(3);

    // Cache for chart history snapshots received from BeforeHistoryCleared event
    private static Dictionary<long, List<Database.ChartDataPoint>>? _cachedDpsHistory;
    private static Dictionary<long, List<Database.ChartDataPoint>>? _cachedHpsHistory;

    /// <summary>
    /// Initialize database integration with DataStorage
    /// </summary>
    public static async Task InitializeDatabaseAsync(
        IDataStorage? dataStorage = null,
        string? databasePath = null,
        object? chartDataService = null,
        bool autoCleanup = true,
        int maxEncounters = 20,
        double maxSizeMB = 100)
    {
        if (_isInitialized) return;

        databasePath ??= DatabaseInitializer.GetDefaultDatabasePath();
        _dataStorage = dataStorage;
        _chartDataService = chartDataService; // Store reference to chart service

        // Initialize database with cleanup settings
        await DatabaseInitializer.InitializeAsync(databasePath, autoCleanup, maxEncounters, maxSizeMB);

        // Create encounter service
        var contextFactory = DatabaseInitializer.CreateContextFactory(databasePath);
        _encounterService = new EncounterService(contextFactory);

        // Subscribe to ChartDataService events (if available)
        if (_chartDataService != null)
        {
            try
            {
                var serviceType = _chartDataService.GetType();
                var eventInfo = serviceType.GetEvent("BeforeHistoryCleared");

                if (eventInfo != null)
                {
                    // Create delegate and subscribe to event
                    var handlerType = eventInfo.EventHandlerType;
                    var handler = Delegate.CreateDelegate(handlerType!, typeof(DataStorageExtensions).GetMethod(nameof(OnChartHistoryClearing),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!);
                    eventInfo.AddEventHandler(_chartDataService, handler);

                    Console.WriteLine("[DataStorageExtensions] Successfully subscribed to ChartDataService.BeforeHistoryCleared event");
                }
                else
                {
                    Console.WriteLine("[DataStorageExtensions] WARNING: BeforeHistoryCleared event not found on ChartDataService");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataStorageExtensions] ERROR subscribing to ChartDataService events: {ex.Message}");
            }
        }

        // Subscribe to DataStorage events
        if (_dataStorage != null)
        {
            // Use IDataStorage instance (DataStorageV2)
            _dataStorage.NewSectionCreated += OnNewSectionCreated;
            _dataStorage.ServerConnectionStateChanged += OnServerConnectionStateChanged;
            _dataStorage.PlayerInfoUpdated += OnPlayerInfoUpdated;
        }
        else
        {
            // Fallback to static DataStorage
            DataStorage.NewSectionCreated += OnNewSectionCreated;
            DataStorage.ServerConnectionStateChanged += OnServerConnectionStateChanged;
            DataStorage.PlayerInfoUpdated += OnPlayerInfoUpdated;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Get the encounter service instance
    /// </summary>
    public static EncounterService? GetEncounterService() => _encounterService;

    /// <summary>
    /// Start a new encounter manually
    /// </summary>
    public static async Task StartNewEncounterAsync()
    {
        if (_encounterService == null) return;

        await _encounterService.StartEncounterAsync();
    }

    /// <summary>
    /// Save current encounter to database
    /// </summary>
    public static async Task SaveCurrentEncounterAsync()
    {
        if (_encounterService == null) return;

        // Lazy start: If no encounter is active, start one now
        if (!_encounterService.IsEncounterActive)
        {
            await StartNewEncounterAsync();
            Console.WriteLine("[DataStorageExtensions] Started new encounter (lazy initialization)");
        }

        // Avoid saving too frequently
        if (DateTime.Now - _lastSaveTime < MinSaveDuration) return;

        try
        {
            Dictionary<long, PlayerInfo> playerInfos;
            Dictionary<long, DpsData> dpsData;

            if (_dataStorage != null)
            {
                // Use IDataStorage instance
                playerInfos = _dataStorage.ReadOnlyPlayerInfoDatas.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                dpsData = _dataStorage.ReadOnlySectionedDpsDatas.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
                // Fallback to static DataStorage
                playerInfos = DataStorage.ReadOnlyPlayerInfoDatas.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                dpsData = DataStorage.ReadOnlySectionedDpsDatas.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            // Filter out only very minimal encounters (accidental hits, etc.)
            // Very low threshold (10K) to capture trash mobs and small fights
            // The main deduplication happens in RegisterBossEngagement (1 encounter per session)
            var totalDamage = dpsData.Values.Sum(d => d.TotalAttackDamage);
            if (totalDamage < 10_000)
            {
                Console.WriteLine($"[DataStorageExtensions] Skipping save - total damage {totalDamage:N0} is below 10K threshold (likely accidental hit)");
                return;
            }

            // Get chart history from cache (populated by BeforeHistoryCleared event)
            Dictionary<long, List<Database.ChartDataPoint>>? dpsHistory = _cachedDpsHistory;
            Dictionary<long, List<Database.ChartDataPoint>>? hpsHistory = _cachedHpsHistory;

            if (dpsHistory != null && hpsHistory != null)
            {
                Console.WriteLine($"[DataStorageExtensions] Using cached chart history from BeforeHistoryCleared event:");
                Console.WriteLine($"  - DPS History: {dpsHistory.Count} players, {dpsHistory.Sum(kvp => kvp.Value.Count)} total points");
                Console.WriteLine($"  - HPS History: {hpsHistory.Count} players, {hpsHistory.Sum(kvp => kvp.Value.Count)} total points");
            }
            else
            {
                Console.WriteLine("[DataStorageExtensions] WARNING: No cached chart history available!");
                Console.WriteLine("  This may be normal if:");
                Console.WriteLine("    1. This is the first save before any combat");
                Console.WriteLine("    2. The BeforeHistoryCleared event hasn't fired yet");
                Console.WriteLine("    3. There was no chart data to save");
            }

            await _encounterService.SavePlayerStatsAsync(playerInfos, dpsData, dpsHistory, hpsHistory);

            // Clear cache after successful save
            _cachedDpsHistory = null;
            _cachedHpsHistory = null;

            _lastSaveTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving encounter: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert chart history from WPF ChartDataPoint to Core ChartDataPoint
    /// </summary>
    private static Dictionary<long, List<Database.ChartDataPoint>>? ConvertChartHistory(dynamic? wpfHistory)
    {
        if (wpfHistory == null) return null;

        var result = new Dictionary<long, List<Database.ChartDataPoint>>();

        foreach (var kvp in wpfHistory)
        {
            long playerId = kvp.Key;
            var points = new List<Database.ChartDataPoint>();

            foreach (var wpfPoint in kvp.Value)
            {
                // Access properties via dynamic
                DateTime timestamp = wpfPoint.Timestamp;
                double value = wpfPoint.Value;
                points.Add(new Database.ChartDataPoint(timestamp, value));
            }

            result[playerId] = points;
        }

        return result;
    }

    /// <summary>
    /// End current encounter and save to database
    /// </summary>
    public static async Task EndCurrentEncounterAsync(long durationMs, string? bossName = null, long? bossUuid = null)
    {
        if (_encounterService == null) return;

        // Final save before ending
        await SaveCurrentEncounterAsync();

        // Save all encounters longer than 1 second (includes trash mobs and boss fights)
        // Very short encounters (< 1s) are likely accidental hits and will be deleted
        const long MinEncounterDurationMs = 1000; // 1 second
        await _encounterService.EndCurrentEncounterAsync(durationMs, MinEncounterDurationMs, bossName, bossUuid);
    }

    /// <summary>
    /// Get recent encounters for history
    /// </summary>
    public static async Task<System.Collections.Generic.List<EncounterSummary>> GetRecentEncountersAsync(int count = 50)
    {
        if (_encounterService == null) return new System.Collections.Generic.List<EncounterSummary>();

        return await _encounterService.GetRecentEncountersAsync(count);
    }

    /// <summary>
    /// Load encounter from database
    /// </summary>
    public static async Task<EncounterData?> LoadEncounterAsync(string encounterId)
    {
        if (_encounterService == null) return null;

        return await _encounterService.LoadEncounterAsync(encounterId);
    }

    /// <summary>
    /// Get cached player info from database to fix "Unknown" players
    /// </summary>
    public static async Task<PlayerInfo?> GetCachedPlayerInfoAsync(long uid)
    {
        if (_encounterService == null) return null;

        return await _encounterService.GetCachedPlayerInfoAsync(uid);
    }

    /// <summary>
    /// Cleanup old encounters from database
    /// </summary>
    public static async Task CleanupOldEncountersAsync(int keepCount = 20)
    {
        if (_encounterService == null) return;

        await _encounterService.CleanupOldEncountersAsync(keepCount);
    }

    /// <summary>
    /// Delete all encounters from database
    /// </summary>
    public static async Task<int> DeleteAllEncountersAsync()
    {
        if (_encounterService == null) return 0;

        return await _encounterService.DeleteAllEncountersAsync();
    }

    // Event handlers

    /// <summary>
    /// Event handler for ChartDataService.BeforeHistoryCleared
    /// Receives chart history snapshots BEFORE they are cleared
    /// </summary>
    private static void OnChartHistoryClearing(object? sender, object eventArgs)
    {
        try
        {
            Console.WriteLine("[DataStorageExtensions] OnChartHistoryClearing event received");

            // Extract snapshots from event args using reflection (cross-assembly)
            var eventArgsType = eventArgs.GetType();
            var dpsProperty = eventArgsType.GetProperty("DpsHistorySnapshot");
            var hpsProperty = eventArgsType.GetProperty("HpsHistorySnapshot");

            if (dpsProperty != null && hpsProperty != null)
            {
                var wpfDpsHistory = dpsProperty.GetValue(eventArgs) as dynamic;
                var wpfHpsHistory = hpsProperty.GetValue(eventArgs) as dynamic;

                // Convert WPF ChartDataPoints to Core ChartDataPoints
                _cachedDpsHistory = ConvertChartHistory(wpfDpsHistory);
                _cachedHpsHistory = ConvertChartHistory(wpfHpsHistory);

                Console.WriteLine($"[DataStorageExtensions] Chart history cached from event:");
                Console.WriteLine($"  - DPS History: {_cachedDpsHistory?.Count ?? 0} players, {_cachedDpsHistory?.Sum(kvp => kvp.Value.Count) ?? 0} total points");
                Console.WriteLine($"  - HPS History: {_cachedHpsHistory?.Count ?? 0} players, {_cachedHpsHistory?.Sum(kvp => kvp.Value.Count) ?? 0} total points");
            }
            else
            {
                Console.WriteLine("[DataStorageExtensions] ERROR: Could not find snapshot properties in event args");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataStorageExtensions] ERROR in OnChartHistoryClearing: {ex.Message}");
            Console.WriteLine($"  Stack trace: {ex.StackTrace}");
        }
    }

    private static void OnNewSectionCreated()
    {
        try
        {
            // NEW DATA FLOW: Encounters are now saved BEFORE new combat starts (in RegisterBossEngagement)
            // This event is fired when combat ends so UI can enter "Last Battle" mode
            // Data stays in memory until new combat begins, then it's saved and cleared

            // No save here anymore! Saving happens in DataStorageV2.RegisterBossEngagement()
            // when the next boss fight is about to start.

            Console.WriteLine("[DataStorageExtensions] NewSectionCreated: Combat ended, UI can show Last Battle");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling new section: {ex.Message}");
        }
    }

    private static async void OnServerConnectionStateChanged(bool isConnected)
    {
        try
        {
            // Boss fights now handle their own encounter lifecycle in DataStorageV2
            // Only clean up on disconnect

            if (!isConnected)
            {
                // End encounter when server disconnects (if any active)
                if (_encounterService != null && _encounterService.IsEncounterActive)
                {
                    await SaveCurrentEncounterAsync();
                    await _encounterService.EndCurrentEncounterAsync(0);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling server connection change: {ex.Message}");
        }
    }

    private static async void OnPlayerInfoUpdated(PlayerInfo playerInfo)
    {
        try
        {
            // Update player cache in database
            // Note: We do NOT save encounter here - encounters are only saved when combat ends
            // (in RegisterBossEngagement when a new boss fight starts)
            if (_encounterService != null)
            {
                await _encounterService.UpdatePlayerCacheAsync(playerInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating player info in database: {ex.Message}");
        }
    }

    /// <summary>
    /// Preload player cache from database to reduce "Unknown" players
    /// </summary>
    public static async Task PreloadPlayerCacheAsync()
    {
        if (_encounterService == null) return;

        try
        {
            using var context = DatabaseInitializer.CreateContextFactory(DatabaseInitializer.GetDefaultDatabasePath())();
            var repository = new EncounterRepository(context);
            var players = await repository.GetAllPlayersAsync();

            int loadedCount = 0;
            foreach (var player in players)
            {
                // Only preload players with names (skip NPCs and incomplete data)
                if (!string.IsNullOrEmpty(player.Name) && player.Name != "Unknown" && !player.IsNpc)
                {
                    var playerInfo = new PlayerInfo
                    {
                        UID = player.UID,
                        Name = player.Name,
                        ProfessionID = player.ProfessionID,
                        SubProfessionName = player.SubProfessionName,
                        Spec = player.Spec,
                        CombatPower = player.CombatPower,
                        Level = player.Level,
                        RankLevel = player.RankLevel,
                        Critical = player.Critical,
                        Lucky = player.Lucky,
                        MaxHP = player.MaxHP
                    };

                    // Add to DataStorage without triggering events
                    DataStorage.ReadOnlyPlayerInfoDatas.GetType()
                        .GetField("_dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                        .GetValue(DataStorage.ReadOnlyPlayerInfoDatas);

                    // Use reflection to access private PlayerInfoDatas dictionary
                    var playerInfoDatasField = typeof(DataStorage).GetField("PlayerInfoDatas",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (playerInfoDatasField != null)
                    {
                        var playerInfoDatas = playerInfoDatasField.GetValue(null) as Dictionary<long, PlayerInfo>;
                        if (playerInfoDatas != null && !playerInfoDatas.ContainsKey(player.UID))
                        {
                            playerInfoDatas[player.UID] = playerInfo;
                            loadedCount++;
                        }
                    }
                }
            }

            Console.WriteLine($"Preloaded {loadedCount} players from database cache");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error preloading player cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleanup database integration
    /// </summary>
    public static void Shutdown()
    {
        if (!_isInitialized) return;

        // Unsubscribe from ChartDataService events
        if (_chartDataService != null)
        {
            try
            {
                var serviceType = _chartDataService.GetType();
                var eventInfo = serviceType.GetEvent("BeforeHistoryCleared");

                if (eventInfo != null)
                {
                    var handlerType = eventInfo.EventHandlerType;
                    var handler = Delegate.CreateDelegate(handlerType!, typeof(DataStorageExtensions).GetMethod(nameof(OnChartHistoryClearing),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!);
                    eventInfo.RemoveEventHandler(_chartDataService, handler);

                    Console.WriteLine("[DataStorageExtensions] Unsubscribed from ChartDataService.BeforeHistoryCleared event");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataStorageExtensions] ERROR unsubscribing from ChartDataService events: {ex.Message}");
            }
        }

        // Unsubscribe from DataStorage events
        if (_dataStorage != null)
        {
            _dataStorage.NewSectionCreated -= OnNewSectionCreated;
            _dataStorage.ServerConnectionStateChanged -= OnServerConnectionStateChanged;
            _dataStorage.PlayerInfoUpdated -= OnPlayerInfoUpdated;
        }
        else
        {
            DataStorage.NewSectionCreated -= OnNewSectionCreated;
            DataStorage.ServerConnectionStateChanged -= OnServerConnectionStateChanged;
            DataStorage.PlayerInfoUpdated -= OnPlayerInfoUpdated;
        }

        // Clear cached data
        _cachedDpsHistory = null;
        _cachedHpsHistory = null;

        _encounterService = null;
        _dataStorage = null;
        _chartDataService = null;
        _isInitialized = false;
    }
}
