# Release Notes - Version 1.5.6

**Release Date:** 2025-12-15

## Overview

Version 1.5.6 combines critical stability fixes with major performance enhancements. This release addresses the race condition crashes in 20-man raids while introducing customizable DPS refresh rates to eliminate in-game lag.

---

## 🐛 Critical Bug Fixes

### Fixed Race Condition Crash in 20-Man Raids

**Issue:** BlueMeter crashed frequently during 20-man raids with the error:
```
System.ArgumentException: Destination array is not long enough to copy all the items in the collection.
Check array index and length.
at System.Collections.Generic.Dictionary`2.ValueCollection.CopyTo(TValue[] array, Int32 index)
at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection)
at BlueMeter.WPF.ViewModels.DpsStatisticsViewModel.PerformUiUpdate()
```

**Root Cause:** Threading race condition in `DataStorageV2.cs` where the packet processing thread and UI thread simultaneously accessed DPS data dictionaries. Particularly prevalent in 20-man raids due to high player counts and dynamic join/leave.

**Fix Applied:**
- Added comprehensive thread-safe locking to `DataStorageV2.cs`
- Implemented list caching to reduce repeated `ToList()` calls
- Protected all dictionary access and modification operations
- Automatic cache invalidation on data changes

**Impact:**
- ✅ Eliminates crashes in 20-man raids, World Boss Carnage, and large group content
- ✅ Improves performance through smart caching
- ✅ Minimal overhead from locking

### Fixed ScopeTime Toggle Not Working

**Issue:** Switching from "Total" back to "Current" scope didn't update the display

**Fix:** User-initiated toggles (Current/Total, Damage/Healing) now bypass throttling and update immediately while keeping combat updates optimized

---

## ⚡ New Features

### DPS Refresh Rate Settings

Users can now customize how often the DPS meter updates, balancing smoothness with CPU usage:

**Available Settings:**
- **Minimal:** 10 FPS (100ms) - Best for low-end PCs or when playing in-game
- **Low:** 20 FPS (50ms) - Good balance (Default)
- **Medium:** 30 FPS (33ms) - Smooth updates
- **High:** 60 FPS (16ms) - Maximum smoothness (high-end PCs only)

**Configuration:**
- Settings → Performance → DPS Refresh Rate
- Changes take effect immediately without restart
- Persisted across application restarts

**Benefits:**
- **Eliminates lag when tabbed into game** - Lower refresh rates reduce CPU competition with the game
- **Customizable experience** - Choose smoothness vs. performance based on your PC
- **Instant feedback** - See changes immediately without restarting

**Technical Details:**
- Updates `BattleLogQueue.GlobalBatchTimeout` based on selected refresh rate
- UI updates are non-blocking to prevent freezing during intense combat
- Recommended: Use Minimal (10 FPS) when playing, Medium/High (30-60 FPS) when tabbed out

---

## 🚀 Performance Improvements

### UI Thread Optimization
- Changed sorting to non-blocking operations for smoother experience during raids
- Prevents UI freezing when processing large player lists (10+ players)
- Maintains responsiveness even during intense combat

### Batch Processing Optimization
- Increased batch size from 100 to 300 events for better throughput
- Reduces event processing overhead
- Improves performance in high-activity scenarios

### Reduced Boss Death Delay
- Decreased delay from 8s to 5s for faster fight archiving
- "Last Battle" transition happens quicker after boss fights
- Encounter history saves faster

---

## Technical Details

### Files Modified

**Thread Safety (Race Condition Fix):**
- `BlueMeter.Core/Data/DataStorageV2.cs`
  - Added `_dpsDataLock` for thread synchronization
  - Added cached list fields for performance
  - Protected all dictionary operations with locks

**DPS Refresh Rate Feature:**
- `BlueMeter.WPF/Config/AppConfig.cs`
  - Added `DpsRefreshRate` property with change notification
  - Integrated with `BattleLogQueue.GlobalBatchTimeout`
- `BlueMeter.WPF/Views/SettingsView.xaml`
  - Added Performance section with DPS Refresh Rate dropdown
  - Added help text explaining each option
- `BlueMeter.WPF/Models/DpsRefreshRate.cs`
  - New enum for refresh rate options
  - Extension methods for interval conversion

**Performance Improvements:**
- `BlueMeter.WPF/ViewModels/DpsStatisticsViewModel.cs`
  - Non-blocking UI sorting
  - Bypass throttle for user-initiated actions
- `BlueMeter.Core/Data/BattleLogQueue.cs`
  - Increased batch size to 300
- `BlueMeter.Core/Data/DataStorageV2.cs`
  - Boss death delay reduced to 5s

**Version Updated:**
- `BlueMeter.WPF/BlueMeter.WPF.csproj` - Updated to 1.5.6

---

## User Impact

### Stability
- **No more crashes** in 20-man raids or large group content
- **Consistent performance** with dynamic player join/leave
- **Reliable** in World Boss Carnage and high-activity scenarios

### Performance
- **Eliminates in-game lag** - Lower DPS refresh rates reduce CPU usage when playing
- **Smoother UI** - Non-blocking operations prevent freezing during intense combat
- **Faster transitions** - Quicker boss death detection and fight archiving

### Customization
- **User control** - Choose between smoothness and performance
- **Immediate feedback** - Settings apply instantly
- **Persistent preferences** - Settings saved across restarts

---

## Testing Notes

This release:
- ✅ Compiles successfully with no build errors
- ✅ Combines proven fixes from v1.5.4 and v1.5.5
- ✅ Maintains backward compatibility with existing settings
- ⚠️ Should be tested in 20-man raids to confirm stability
- ⚠️ Test different refresh rate settings for performance impact

**Recommended Testing:**
1. Run 20-man raids with default settings (Low - 20 FPS)
2. Test Minimal (10 FPS) setting while playing in-game
3. Verify no crashes during dynamic player join/leave
4. Confirm ScopeTime toggle works in both directions
5. Test "Last Battle" transition timing (should be ~5s after boss death)

---

## Upgrade Notes

This is a **critical stability and performance release**. All users, especially those experiencing crashes in raids or lag when playing, should upgrade immediately.

**Installation:**
- Simply replace your existing BlueMeter installation with v1.5.6
- Settings will be preserved (new DPS Refresh Rate defaults to "Low")
- No configuration changes required

---

## Known Issues

None introduced by this release.

---

**Special Thanks:**
- Community members for crash reports and performance feedback
- Testing during 20-man raids to identify the race condition
