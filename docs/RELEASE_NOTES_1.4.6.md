# BlueMeter v1.4.6 - Critical Bug Fixes

**Release Date:** December 8, 2025
**Branch:** `main`
**Status:** ✅ Released

---

## 🐛 Critical Bug Fixes

### Meter Freeze After Timeout Fix

Fixed critical issue where the DPS meter would remain frozen and stop accepting new combat data after a timeout occurred followed by a manual reset.

**Root Cause:**
- When combat timeout occurred, `_timeoutSectionClearedOnce` flag was set to true
- Manual reset via `ResetData()` or `ResetFullData()` did not reset this flag
- Meter remained in "timeout cleared" state, blocking all new data ingestion
- Only way to recover was to wait for new battle logs to trigger a section change

**Impact:**
- Users experiencing timeouts had to restart BlueMeter to get meter working again
- Manual reset button became ineffective after any timeout
- Severely impacted user experience in long play sessions

**Solution:**
- Added `_timeoutSectionClearedOnce = false` reset in both `ResetData()` and `ResetFullData()` methods
- Wrapped in proper `_sectionTimeoutLock` synchronization
- Meter now immediately accepts new data after manual reset, regardless of previous timeout state

**Files Changed:**
- `BlueMeter.Core/Data/DataStorageV2.cs` (Lines 954-962, 1108-1116)

**Technical Details:**
```csharp
// CRITICAL FIX: Reset timeout flag to allow meter to accept new data after manual reset
// Without this, if timeout occurred before reset, meter stays frozen until new battle logs arrive
lock (_sectionTimeoutLock)
{
    _timeoutSectionClearedOnce = false;
}
```

---

### Plugin Enable/Disable Logic Fix

Fixed issue where only the DPS Plugin was enabled, blocking access to all other plugins including History, Settings, etc.

**Root Cause:**
- Previous code: `public bool IsEnabled => Plugin.GetType().Name == "DpsPlugin";`
- This hardcoded check disabled all plugins except DpsPlugin
- Users could not access Chart History, Advanced Settings, or other features

**Solution:**
- Changed to: `public bool IsEnabled => Plugin.PackageName != "BlueMeter.WPF.Plugins.BuiltIn.ModuleSolverPlugin";`
- All plugins now enabled except Module Solver (intentionally disabled - under development)
- Restores full functionality of all production-ready plugins

**Files Changed:**
- `BlueMeter.WPF/ViewModels/PluginListItemViewModel.cs` (Line 37)

---

## ✨ User Experience Improvements

**Timeout Recovery:**
- ✅ Manual reset now works correctly after timeouts
- ✅ No need to restart BlueMeter after timeout events
- ✅ Immediate data ingestion after reset button click

**Plugin Access:**
- ✅ Chart History accessible again
- ✅ Settings panel accessible again
- ✅ All production-ready plugins enabled
- ⚠️ Module Solver temporarily disabled (under development)

---

## 🔧 Configuration

No configuration changes required. All fixes are automatic.

---

## 🚀 Migration Guide

### From v1.4.5 to v1.4.6

**No Breaking Changes!**
- ✅ Fully backward compatible
- ✅ Automatic fixes - no user action required
- ✅ Existing configuration preserved
- ✅ All features from v1.4.5 retained

---

## ⚠️ Known Issues

**Module Solver Plugin:**
- Temporarily disabled while under development
- Will be re-enabled in future release after testing

---

## 🛠️ Technical Details

**Build Information:**
- .NET Version: 8.0
- Target Framework: net8.0-windows
- Build Status: ✅ 0 Errors, 0 Warnings

**Files Changed:** 2 files
- Modified files: 2
- Lines added: ~16
- Lines removed: ~2

---

## 💬 Feedback & Support

If you encounter any issues:
1. Report issues on GitHub: https://github.com/caaatto/BlueMeter
2. Provide logs from `BlueMeter.WPF/bin/Release/net8.0-windows/logs/`

---

**Full Changelog:** See commits on `main` branch

**Previous Version:** v1.4.5
**Next Version:** v1.5.0 (TBD - Skill Breakdown & Module Solver)
