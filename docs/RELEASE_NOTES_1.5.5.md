# Release Notes - Version 1.5.5

**Release Date:** 2025-12-15

## Critical Bug Fix

### 🐛 Fixed Race Condition Crash in 20-Man Raids

**Issue:** BlueMeter crashed frequently during 20-man raids with the error:
```
System.ArgumentException: Destination array is not long enough to copy all the items in the collection.
Check array index and length.
at System.Collections.Generic.Dictionary`2.ValueCollection.CopyTo(TValue[] array, Int32 index)
at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection)
at BlueMeter.WPF.ViewModels.DpsStatisticsViewModel.PerformUiUpdate() in DpsStatisticsViewModel.cs:line 384
```

**Root Cause:** A threading race condition in `DataStorageV2.cs` where:
- **Packet thread:** Processing combat data → modifying DPS dictionaries → firing UI update events
- **UI thread:** Receiving events → reading DPS data → attempting to create snapshot list from dictionary
- **Collision:** Dictionary was modified (players joining/leaving in 20-man raids) while being copied to a list, causing the crash

The issue was particularly prevalent in 20-man raids due to:
- High player count (more dictionary entries)
- Players frequently joining/leaving combat (dynamic dictionary modifications)
- High combat activity (frequent UI updates)
- Increased probability of thread collision

**Why This Wasn't Fixed in v1.5.2:**
Version 1.5.2 added thread-safe locking to `DataStorage.cs` (the old static implementation), but the project uses `DataStorageV2.cs` (the newer instance-based implementation), which didn't have the same protections.

**Fix Applied:**
Added comprehensive thread-safe locking to `DataStorageV2.cs`:
- Added `_dpsDataLock` object for thread synchronization
- Added cached list fields (`_cachedFullDpsDataList`, `_cachedSectionedDpsDataList`)
- Protected `ReadOnlyFullDpsDataList` and `ReadOnlySectionedDpsDataList` properties with locks and caching
- Protected dictionary modifications in `GetOrCreateDpsDataByUid()` with locks
- Protected dictionary clearing in `PrivateClearDpsDataNoEvents()`, `PrivateClearDpsData()`, and `ClearAllDpsData()` with locks
- Automatic cache invalidation when dictionaries are modified

**Impact:**
- **Eliminates crashes** during 20-man raids and other high-player-count scenarios
- **Improves performance** through list caching (reduces repeated `ToList()` calls)
- **Minimal overhead** from locking (only when accessing/modifying DPS data)
- **Fixes instability** in World Boss Carnage, raids, and large group content

## Technical Details

### Files Changed

**Modified:**
- `BlueMeter.Core/Data/DataStorageV2.cs`
  - Added `_dpsDataLock` object for thread synchronization (line 21)
  - Added cached list fields (lines 153, 163)
  - Protected `ReadOnlyFullDpsDataList` property with lock and caching (lines 196-209)
  - Protected `ReadOnlySectionedDpsDataList` property with lock and caching (lines 219-232)
  - Protected `GetOrCreateDpsDataByUid()` method with lock (lines 746-768)
  - Protected `PrivateClearDpsDataNoEvents()` method with lock (lines 956-968)
  - Protected `PrivateClearDpsData()` method with lock (lines 1031-1054)
  - Protected `ClearAllDpsData()` method with lock (lines 1201-1216)

**Version Updated:**
- `BlueMeter.WPF/BlueMeter.WPF.csproj`
  - Updated `<Version>` from 1.5.3 to 1.5.5

### Thread Safety Pattern

The fix implements the same pattern as `DataStorage.cs` from v1.5.2:

1. **Lock all dictionary access** - Prevent concurrent reads during writes
2. **Cache list snapshots** - Avoid repeated `ToList()` calls which are expensive and vulnerable
3. **Invalidate cache on modification** - Ensure fresh data after dictionary changes
4. **Minimal lock scope** - Only lock critical sections to minimize performance impact

```csharp
// Before (vulnerable to race conditions)
public IReadOnlyList<DpsData> ReadOnlySectionedDpsDataList =>
    SectionedDpsData.Values.ToList().AsReadOnly();

// After (thread-safe with caching)
public IReadOnlyList<DpsData> ReadOnlySectionedDpsDataList
{
    get
    {
        lock (_dpsDataLock)
        {
            if (_cachedSectionedDpsDataList == null)
            {
                _cachedSectionedDpsDataList = SectionedDpsData.Values.ToList();
            }
            return _cachedSectionedDpsDataList.AsReadOnly();
        }
    }
}
```

## Testing Notes

This fix:
- ✅ Compiles successfully with no build errors
- ✅ Addresses the exact exception from the crash report
- ✅ Uses proven pattern from DataStorage.cs v1.5.2
- ⚠️ Should be tested in 20-man raids to confirm stability

**Testing Recommendations:**
1. Run multiple consecutive 20-man raids
2. Monitor for crashes during high player activity
3. Verify DPS tracking accuracy with dynamic player join/leave
4. Test in World Boss Carnage with large player counts

## Known Issues

None introduced by this release.

## Upgrade Notes

This is a critical hotfix release. Users experiencing crashes in 20-man raids should upgrade immediately. Simply replace your existing BlueMeter installation with v1.5.5.

---

**Special Thanks:**
- Original crash report provided by the user for detailed debugging
