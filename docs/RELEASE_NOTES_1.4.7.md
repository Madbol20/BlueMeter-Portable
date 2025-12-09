# BlueMeter v1.4.7 - Last Battle Fix & Module Solver Development

**Release Date:** December 8, 2025
**Branch:** `main`
**Status:** ✅ Released

---

## 🐛 Critical Bug Fix

### Meter Stuck in "Last Battle" Mode Fix

Fixed critical issue where the DPS meter would remain stuck displaying "[Last]" battle data after manual reset, preventing new combat data from being shown.

**Root Cause:**
- When user clicked "View Last Battle", meter switched to `IsShowingLastBattle = true` mode
- Manual reset via "Reset Section" button did not clear this state
- Meter remained frozen showing old data with "[Last]" label
- New combat data was being processed but not displayed

**Impact:**
- Users had to restart BlueMeter to recover from stuck Last Battle mode
- Manual reset button appeared broken after viewing last battle
- Confusing UX - meter showed "[Last]" even during active combat

**Solution:**
- Added state clearing in `ResetSection()` method:
  - `_awaitingSectionStart = false`
  - `IsShowingLastBattle = false`
  - `BattleStatusLabel = string.Empty`
  - `_lastBattleDataSnapshot = null`
- Meter now properly returns to live mode after manual reset

**Files Changed:**
- `BlueMeter.WPF/ViewModels/DpsStatisticsViewModel.cs` (Lines 266-273)

**Technical Details:**
```csharp
// CRITICAL FIX: Clear "Last Battle" state to prevent meter from being stuck in [Last] mode
// Previously, if meter was in Last Battle mode and user hit ResetSection, it would stay frozen
_awaitingSectionStart = false;
IsShowingLastBattle = false;
BattleStatusLabel = string.Empty;
_lastBattleDataSnapshot = null;
```

---

## 🔧 Development Changes (Module Solver - Disabled)

**Note:** All Module Solver features remain disabled in the UI. These changes are development work-in-progress and do not affect normal meter functionality.

### OCR Capture Service

Added `ModuleOCRCaptureService` for automatic module detection via screen capture:
- Captures module stats from game UI using OCR
- Timer-based periodic capture (1 second intervals)
- Automatic duplicate detection and filtering
- Session-based capture management

**Files Added:**
- `BlueMeter.WPF/Services/ModuleSolver/ModuleOCRCaptureService.cs`

### Network Device Selection

Enhanced packet capture with network device selection:
- List all available network interfaces
- Auto-select optimal device (non-Bluetooth, non-Virtual)
- Manual device refresh capability
- Better error handling for no device scenarios

### UI Enhancements

**Added InverseBooleanConverter:**
- `BlueMeter.WPF/Converters/InverseBooleanConverter.cs`
- Enables/disables UI controls based on inverted boolean states
- Used for Start/Stop button toggling

**Module Solver UI Updates:**
- Network device dropdown with refresh button
- Packet Capture controls (Start/Stop)
- OCR Capture controls (Start/Stop)
- Visual feedback for capture status

**Files Changed:**
- `BlueMeter.WPF/Views/ModuleSolveView.xaml` (Lines 516-606)
- `BlueMeter.WPF/ViewModels/ModuleSolveViewModel.cs` (Lines 19-441)
- `BlueMeter.WPF/Extensions/ModuleSolverServiceExtensions.cs` (Line 14)

---

## ✨ User Experience

**Last Battle Mode:**
- ✅ Manual reset now properly exits Last Battle view
- ✅ Meter immediately shows live combat data after reset
- ✅ No need to restart BlueMeter after viewing last battle
- ✅ Clear visual feedback of current meter state

**Module Solver:**
- ⚠️ Feature remains disabled in UI (not accessible to users)
- 🔧 Development continues for future release
- 📊 No impact on normal DPS meter functionality

---

## 🔧 Configuration

No configuration changes required. All fixes are automatic.

---

## 🚀 Migration Guide

### From v1.4.6 to v1.4.7

**No Breaking Changes!**
- ✅ Fully backward compatible
- ✅ Automatic fixes - no user action required
- ✅ Existing configuration preserved
- ✅ All features from v1.4.6 retained

---

## ⚠️ Known Issues

**Module Solver Plugin:**
- Temporarily disabled while under development
- OCR capture service is WIP and not production-ready
- Will be re-enabled in future release after thorough testing

---

## 🛠️ Technical Details

**Build Information:**
- .NET Version: 8.0
- Target Framework: net8.0-windows
- Build Status: ✅ 0 Errors, 0 Warnings

**Files Changed:** 6 files
- Modified files: 5
- New files: 2
- Lines added: ~220
- Lines removed: ~2

---

## 💬 Feedback & Support

If you encounter any issues:
1. Report issues on GitHub: https://github.com/caaatto/BlueMeter
2. Provide logs from `BlueMeter.WPF/bin/Release/net8.0-windows/logs/`

---

**Full Changelog:** See commits on `main` branch

**Previous Version:** v1.4.6
**Next Version:** v1.5.0 (TBD - Module Solver Release)
