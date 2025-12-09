# Release Notes - Version 1.5.2

**Release Date:** 2025-12-09

## Critical Bug Fix

### 🐛 Fixed Race Condition Crash During Back-to-Back Raids

**Issue:** BlueMeter would crash after 2-3 consecutive raids/encounters, especially in passthrough mode with "clear on instance change" enabled.

**Error:** `System.ArgumentException: Destination array is not long enough to copy all the items in the collection`

**Root Cause:** A threading race condition in `DataStorage.cs` where the packet processing thread and UI thread were simultaneously accessing DPS data dictionaries:
- Packet thread: Processing combat data → modifying dictionaries → invalidating caches → firing events
- UI thread: Receiving events → reading cached lists → cache was null → attempting to create new list from dictionary
- Collision: Dictionary was modified while being copied to a list, causing the crash

**Fix Applied:**
- Added thread-safe locking (`_dpsDataLock`) to protect all DPS data dictionary operations
- Protected list creation in `ReadOnlyFullDpsDataList` and `ReadOnlySectionedDpsDataList` properties
- Protected dictionary modifications in `GetOrCreateDpsDataByUID()`
- Protected dictionary clearing in `ClearAllDpsData()` and `PrivateClearDpsData()`

**Impact:**
- Eliminates crashes during extended play sessions with multiple consecutive encounters
- Minimal performance overhead from locking
- Fixes instability in high-activity scenarios (raids, World Boss Carnage)

## Files Changed

### Modified
- `BlueMeter.Core/Data/DataStorage.cs`
  - Added `_dpsDataLock` object for thread synchronization
  - Added lock protection to cached list properties (lines 69-76, 102-109)
  - Added lock protection to dictionary modifications (lines 331-353)
  - Added lock protection to dictionary clearing operations (lines 602-641)

## Testing Notes

This fix has been tested with the build system and compiles successfully. Users should test with 2-3 back-to-back raids in passthrough mode with "clear on instance change" enabled to verify stability.

## Known Issues

None introduced by this release.

## Upgrade Notes

This is a hotfix release. Simply replace your existing BlueMeter installation with v1.5.2.

---

**Special Thanks:**
- GitHub user `anguyen2015` for reporting the crash with detailed error logs
