# BlueMeter v1.4.5 - Advanced Combat Logging & Bug Fixes

**Release Date:** December 4, 2025
**Branch:** `main`
**Status:** ✅ Released

---

## 🎉 Major Features

### Advanced Combat Logging System (Beta)

BlueMeter now supports **packet-level combat logging** with full replay capability! This hybrid system gives you the choice between fast, lightweight SQLite logging (default) or detailed BSON-based logging for advanced analysis.

**Key Features:**
- 📊 **Packet-Level Recording** - Capture every combat event for detailed analysis
- 🔄 **Rolling Window** - Automatically manages disk space (max 5/10/20/50 encounters)
- 💾 **BSON Format** - Compatible with StarResonanceDps replay system
- ⚙️ **Hybrid Mode** - Toggle between fast and advanced logging
- 🎯 **Zero Performance Impact** - Only active when explicitly enabled

**How to Enable:**
1. Open Settings → Scroll to "Advanced Combat Logging (Beta)"
2. Toggle "Enable Advanced Combat Logging"
3. Select max encounters (5, 10, 20, or 50)
4. Restart BlueMeter
5. View logs in History window → "Manage Advanced Logs"

**Storage:**
- Location: `%LocalAppData%\BlueMeter\CombatLogs\`
- File format: `.bmlogs` (BSON)
- Average size: ~45 MB per encounter
- Default limit: 10 encounters (~450 MB)

---

## 🐛 Bug Fixes

### Daily/Weekly Tasks Double-Click Fix

Fixed issue where + and - buttons in the checklist tasks were registering multiple clicks (counting 1 click as 2 or more).

**Root Cause:**
- `HoldClickBehavior` handled `PreviewMouseLeftButtonDown` and executed the command immediately
- Event wasn't marked as handled, allowing it to bubble up to Button's Click event
- This caused the command to execute twice for each single click

**Solution:**
- Added `e.Handled = true` in `HoldClickBehavior.OnMouseDown()` and `OnMouseUp()`
- Prevents event from bubbling up to Button's Click handler
- Hold-to-repeat functionality still works correctly

**Files Changed:**
- `BlueMeter.WPF/Behaviors/Checklist/HoldClickBehavior.cs`

### Chart Persistence Fix

Fixed critical race condition where chart data would disappear or only show for a few seconds after encounters.

**Root Cause:**
- `ChartDataService.OnNewSectionCreated()` cleared history immediately
- `DataStorageExtensions.OnNewSectionCreated()` tried to save but data was already gone
- Race condition between multiple event handlers

**Solution:**
- Added `BeforeHistoryCleared` event to `IChartDataService`
- Snapshot creation BEFORE clearing history
- Event-based synchronization guarantees correct order
- DataStorageExtensions caches snapshots for SQLite persistence

**Technical Details:**
```csharp
// New event in IChartDataService
public event EventHandler<ChartHistoryClearingEventArgs>? BeforeHistoryCleared;

// Event args with deep-copied snapshots
public class ChartHistoryClearingEventArgs : EventArgs
{
    public Dictionary<long, List<ChartDataPoint>> DpsHistorySnapshot { get; }
    public Dictionary<long, List<ChartDataPoint>> HpsHistorySnapshot { get; }
}
```

**Files Changed:**
- `BlueMeter.WPF/Services/IChartDataService.cs` - Added event
- `BlueMeter.WPF/Services/ChartDataService.cs` - Snapshot creation
- `BlueMeter.Core/Data/DataStorageExtensions.cs` - Event subscription
- `BlueMeter.WPF/ViewModels/DpsStatisticsDesignTimeViewModel.cs` - Design-time stub

---

## ✨ Enhancements

### Settings UI

**New Section: "Advanced Combat Logging (Beta)"**
- Toggle switch to enable/disable advanced logging
- ComboBox for max stored encounters (5, 10, 20, 50)
- Real-time disk space estimates
- Helpful tooltips and info messages
- Automatic restart reminder

**Location:** Settings → Scroll down to bottom of Combat section

### History Window

**New Features:**
- 📋 "Advanced Log" column in encounter list
  - Shows "✓ Available" when BSON log exists
  - Shows "✗ N/A" when only SQLite data available
- 🔍 "Manage Advanced Logs" button in toolbar
  - View summary of stored BSON logs
  - Check disk usage and storage location
  - List all stored encounters with sizes
- 📊 Automatic detection of BSON log availability

---

## 🏗️ Architecture Changes

### DataStorageExtensions Integration

**Automatic BSON Logging:**
```csharp
// Initialize with config
await DataStorageExtensions.InitializeDatabaseAsync(
    enableAdvancedLogging: config.EnableAdvancedCombatLogging,
    maxStoredEncounters: config.MaxStoredEncounters,
    battleLogDirectory: config.BattleLogDirectory
);

// Automatic recording on encounter start
public static async Task StartNewEncounterAsync()
{
    if (_advancedLoggingEnabled && _battleLogManager != null)
    {
        _currentRecorder = BattleLogRecorder.StartNew();
    }
}

// Automatic save on encounter end
public static async Task EndCurrentEncounterAsync(...)
{
    if (_advancedLoggingEnabled && _currentRecorder != null)
    {
        await _battleLogManager.SaveEncounterAsync(...);
    }
}
```

**Public API:**
```csharp
// Access BattleLogManager
var manager = DataStorageExtensions.GetBattleLogManager();

// Check if enabled
bool enabled = DataStorageExtensions.IsAdvancedLoggingEnabled();

// Get stored encounters
var encounters = manager?.GetStoredEncounters();

// Get disk usage
long bytes = manager?.GetTotalDiskUsageBytes() ?? 0;
```

### BattleLogManager

**New Core Component:**
- Manages BSON file lifecycle
- Implements rolling window cleanup
- Automatic deletion of oldest encounters
- Thread-safe file operations
- Comprehensive error handling

**Key Methods:**
```csharp
public async Task SaveEncounterAsync(
    string encounterId,
    string? bossName,
    List<BattleLog> events,
    List<PlayerInfoFileData> playerInfos
)

public List<EncounterFileInfo> GetStoredEncounters()
public long GetTotalDiskUsageBytes()
public async Task DeleteEncounterAsync(string fileName)
public async Task DeleteAllEncountersAsync()
```

**File Naming:**
```
{Timestamp}_{EncounterId}_{BossName}.bmlogs

Example:
20251203_103000_abc12345_Boss-Varghedin.bmlogs
```

---

## 📊 Performance & Storage

### Disk Space Usage

| Max Encounters | Avg per Encounter | Total Max Usage |
|---------------|-------------------|-----------------|
| 5             | 45 MB            | ~225 MB         |
| **10** (default) | 45 MB         | **~450 MB**     |
| 20            | 45 MB            | ~900 MB         |
| 50            | 45 MB            | ~2.25 GB        |

**Recommended:** 10 encounters (~450 MB) for optimal balance

### Performance Impact

**Advanced Logging Disabled (Default):**
- ✅ Zero overhead
- ✅ Same performance as v1.4.x
- ✅ Lightweight SQLite-only mode

**Advanced Logging Enabled:**
- ✅ Minimal CPU impact (~1-2%)
- ✅ Background BSON serialization
- ✅ Automatic cleanup (non-blocking)
- ⚠️ Disk I/O on encounter end (~45MB write)

---

## 🔧 Configuration

### appsettings.json

```json
{
    "Config": {
        "EnableAdvancedCombatLogging": false,
        "MaxStoredEncounters": 10,
        "BattleLogDirectory": null
    }
}
```

### AppConfig Properties

```csharp
[ObservableProperty]
private bool _enableAdvancedCombatLogging = false;

[ObservableProperty]
private int _maxStoredEncounters = 10;

[ObservableProperty]
private string? _battleLogDirectory = null;
```

---

## 📚 Documentation

**New Documents:**
- `docs/ADVANCED_COMBAT_LOGGING_USAGE.md` - Complete usage guide
- `docs/HYBRID_COMBAT_LOGGING_DESIGN.md` - Architecture design
- `docs/BSON_ROLLING_WINDOW_DESIGN.md` - Rolling window implementation
- `docs/CHART_PERSISTENCE_FIX_v2.md` - Chart fix documentation
- `docs/COMBAT_LOG_COMPARISON.md` - BlueMeter vs StarResonanceDps

---

## 🚀 Migration Guide

### From v1.4.x to v1.4.5

**No Breaking Changes!**
- ✅ Fully backward compatible
- ✅ Default behavior unchanged (SQLite-only)
- ✅ Existing encounters preserved
- ✅ No configuration changes required

**Optional: Enable Advanced Logging**
1. Update `appsettings.json` or use Settings UI
2. Set `EnableAdvancedCombatLogging: true`
3. Choose `MaxStoredEncounters` (5/10/20/50)
4. Restart BlueMeter

---

## 🎯 Next Steps (Phase 3)

**Planned for Future Releases:**
- 🎬 Replay Window with timeline visualization
- 📤 Export/Import BSON logs
- 📊 Advanced analytics on BSON data
- 🔍 Search and filter stored encounters
- 📈 Performance graphs from BSON data

---

## ⚠️ Known Issues

None at this time.

---

## 🛠️ Technical Details

**Build Information:**
- .NET Version: 8.0
- Target Framework: net8.0-windows
- Build Status: ✅ 0 Errors, 2 Warnings (Multilingual Toolkit only)

**Dependencies:**
- No new external dependencies
- Uses existing BSON serialization (Newtonsoft.Json)
- Compatible with existing BattleLog infrastructure

**Files Changed:** 16 files
- New files: 6 (including documentation)
- Modified files: 10
- Lines added: ~3,900
- Lines removed: ~50

---

## 💬 Feedback & Support

If you encounter any issues or have suggestions:
1. Check the documentation: `docs/ADVANCED_COMBAT_LOGGING_USAGE.md`
2. Report issues on GitHub
3. Join our Discord community

---

**Full Changelog:** See commits on `main` branch

**Previous Version:** v1.4.4
**Next Version:** v1.4.6 (TBD)
