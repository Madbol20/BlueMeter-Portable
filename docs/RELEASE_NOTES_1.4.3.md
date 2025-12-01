# BlueMeter v1.4.3 Release Notes

## 🐛 Bug Fixes

### Queue Pop Alert System Improvements

**Issue**: Queue pop alerts had two critical issues affecting user experience:
1. Alerts continued playing even when the setting was disabled
2. Alerts triggered repeatedly inside dungeons due to false positives

---

## 🔧 Fixes Applied

### 1. Settings Toggle Now Properly Controls Alerts

**Problem**:
- Disabling the "Queue Pop Alerts" toggle in Settings → Alerts (Beta) didn't stop alerts
- The detector kept running and triggering sounds even when disabled

**Fix** (`QueuePopUIDetector.cs:366-383`):
- Added settings check before playing alert sounds
- Checks `QueuePopSoundEnabled` configuration before triggering
- If disabled, alert detection is skipped silently

**Result**:
- ✅ Alerts stop immediately when you disable the slider
- ✅ No more unwanted notifications when feature is turned off

---

### 2. Real-Time Start/Stop on Toggle

**Problem**:
- Changing the alert setting required restarting the application
- The detector didn't respond to live settings changes

**Fix** (`SettingsViewModel.cs:234-251`):
- Added property change handler for `QueuePopSoundEnabled`
- Automatically starts detector when enabled
- Automatically stops detector when disabled

**Result**:
- ✅ Toggle works in real-time without restart
- ✅ Immediate response to user preferences

---

### 3. Smart Startup Based on Settings

**Problem**:
- Detector always started on app launch, regardless of settings
- Wasted resources if user had alerts disabled

**Fix** (`ApplicationStartup.cs:84-100`):
- Check settings before starting detector at startup
- Only initialize OCR and monitoring if alerts are enabled

**Result**:
- ✅ Respects user preferences from the start
- ✅ Saves system resources when feature is disabled

---

### 4. Dungeon False Positive Prevention

**Problem**:
- Alerts triggered repeatedly inside dungeons
- Party UI elements (HP bars, player names with levels) confused the OCR
- Combat UI triggered false positives

**Fix** (`QueuePopUIDetector.cs:54-59`):
- Expanded blacklist patterns to include dungeon/combat UI keywords
- Added patterns: `hp`, `damage`, `dps`, `party`, `objective`, `boss`, `elite`, `mission`
- OCR now filters out combat-related text before detection

**Result**:
- ✅ No more false alerts during dungeon runs
- ✅ Alerts only trigger on actual queue pop UI
- ✅ Improved detection accuracy

---

## 📝 Technical Details

### Files Modified
1. **QueuePopUIDetector.cs**
   - Added `IConfigManager` dependency injection
   - Implemented settings check in `OnQueuePopDetected()`
   - Expanded blacklist with 8 new dungeon-related patterns

2. **SettingsViewModel.cs**
   - Added `IQueuePopUIDetector` to constructor
   - Implemented `QueuePopSoundEnabled` property change handler
   - Added design-time mock implementation

3. **ApplicationStartup.cs**
   - Added conditional startup based on `QueuePopSoundEnabled`
   - Improved logging for disabled state

---

## 🎯 What's Fixed

- ✅ Queue pop alert slider toggle works immediately
- ✅ Alerts stop when disabled (no restart needed)
- ✅ No more false alerts inside dungeons
- ✅ Detector respects settings at app startup
- ✅ Improved OCR blacklist for better accuracy

---

## 📦 Installation

Download the latest release from the [Releases page](https://github.com/caaatto/BlueMeter/releases) and follow the installation instructions in the README.

---

## 🔄 Upgrade Notes

If you're upgrading from v1.4.2:
- Your queue pop alert settings will be preserved
- The feature now works more reliably with proper toggle control
- Dungeon false positives should be eliminated

---

**Thank you for using BlueMeter!** 🎉
