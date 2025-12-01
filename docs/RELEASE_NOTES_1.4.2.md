# BlueMeter v1.4.2 Release Notes

## 🐛 Bug Fixes

### Queue Pop Alert System Fixes

**Issue**: Queue pop alerts were looping continuously and causing application crashes.

**Problems Identified**:
1. **Alert Looping**: OCR detection was triggering repeatedly on "Confirm" button text
2. **Application Crashes**: Memory leaks and threading issues causing instability
3. **Resource Leaks**: GDI handles not properly cleaned up during window capture

**Fixes Applied**:

#### 1. False Positive Prevention (`QueuePopUIDetector.cs:56`)
- **Added "confirm" to blacklist patterns**
- Prevents triggering on standalone Confirm buttons without player cards
- Keeps the working player card detection (Lv.XX pattern) intact
- **Result**: Alert only triggers on actual queue pops, not random UI elements

#### 2. GDI Resource Leak Fix (`QueuePopUIDetector.cs:249-329`)
- **Proper cleanup in finally block**
- Added null checks for all GDI handles before cleanup
- Ensures `DeleteObject()`, `DeleteDC()`, and `ReleaseDC()` are always called
- Prevents handle leaks that could cause crashes
- **Result**: Stable window capture without memory leaks

#### 3. Threading Improvements (`QueuePopUIDetector.cs:191-235`)
- **Replaced async Task with synchronous detection**
- Eliminated potential deadlocks from `Task.Run().Result` pattern
- Added `ObjectDisposedException` handling during shutdown
- **Result**: No more threading-related crashes

#### 4. Improved Disposal Process (`QueuePopUIDetector.cs:367-409`)
- **Waits for OCR operations before cleanup**
- Stops timer before disposing resources
- Graceful timeout handling (2 second max wait)
- Better exception handling during disposal
- **Result**: Clean shutdown without access violations

---

## 🔧 Technical Details

### Root Cause Analysis

**Looping Issue**:
- OCR was correctly detecting player cards with "Lv.60" patterns
- However, screens with Confirm button + some level indicators would also trigger
- No blacklist for "confirm" meant it would repeatedly detect and alert

**Crash Issues**:
1. **GDI Handle Leaks**: Window capture allocated handles but didn't always free them
2. **Task Deadlocks**: Mixing sync/async in timer callbacks caused thread pool exhaustion
3. **Unsafe Disposal**: OCR engine disposed while operations were still running

### Performance Impact
- **Memory**: Eliminated GDI handle leaks (could accumulate to hundreds of leaked handles)
- **Stability**: No more crashes during queue detection or app shutdown
- **Reliability**: 10-second cooldown prevents alert spam

---

## 📝 How It Works Now

Queue pop detection logic:
1. Capture bottom 70% of game window (where player cards appear)
2. Run OCR to extract text
3. Check blacklist (challenge, match, confirm, etc.)
4. Count player cards with "Lv.XX" pattern
5. If 3+ player cards found AND no blacklisted words → Queue pop detected!
6. 10-second cooldown prevents duplicate alerts

---

## 🎯 What's Fixed

- ✅ Queue pop alerts no longer loop continuously
- ✅ Application doesn't crash during queue detection
- ✅ Proper resource cleanup prevents memory leaks
- ✅ Player card detection (Lv.60) still works correctly
- ✅ Multi-language support maintained

---

## 📦 Installation

Download the latest release and extract to your preferred location. The queue pop alert feature can be configured in Settings → Alerts (Beta).

---

**Thank you for using BlueMeter!** 🎉
