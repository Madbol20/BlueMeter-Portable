# BlueMeter Patch Notes

This document contains the changelog for all BlueMeter releases. For detailed release notes, see the `/docs/` directory.

---

## Version 1.4.3

**🐛 Bug Fixes:**
- Fixed queue pop alert toggle - alerts now stop immediately when disabled
- Added real-time start/stop when toggling alert settings (no restart needed)
- Fixed false alerts inside dungeons (expanded OCR blacklist)
- Detector now respects settings at app startup

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.3.md)

---

## Version 1.4.2

**🐛 Hotfix:**
- Fixed queue pop alerts looping continuously
- Fixed application crashes during queue detection
- Proper GDI resource cleanup prevents memory leaks
- Threading improvements eliminate deadlocks
- Improved disposal process for clean shutdown

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.2.md)

---

## Version 1.4.1

**🐛 Hotfix:**
- Fixed missing Tesseract OCR data file (eng.traineddata) in Release builds
- Queue Pop Alerts (Beta) now working correctly in all build configurations

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.1.md)

---

## Version 1.4.0

**🎯 New Features:**
- **Tank/Mitigation Statistics** - Comprehensive tank metrics including:
  - HP Damage Taken vs Shield Damage Absorbed
  - Total Effective Damage (threat tracking)
  - Mitigation Percentage calculation
  - Effective TPS (Threat Per Second)
- **Queue Pop Alerts (Beta)** - Audio notifications when dungeon/raid queue pops
  - OCR-based detection works even when game is minimized
  - Multiple customizable alert sounds
  - Configurable in Settings → Alerts (Beta)

**🔧 Improvements:**
- Removed deprecated WinForms dependencies
- Cleaned up alert system logging
- Code cleanup and optimization

**🐛 Bug Fixes:**
- Fixed damage tracking to properly separate HP damage from shield absorption

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.0.md)

---

For older release notes, see `/docs/RELEASE_NOTES_*.md`
