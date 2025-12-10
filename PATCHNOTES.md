# BlueMeter Patch Notes

This document contains the changelog for all BlueMeter releases. For detailed release notes, see the `/docs/` directory.

---

## Version 1.5.3

**🐛 Bug Fixes:**
- Fixed DPS meter continuing to calculate after combat ends
  - Meter now only tracks player-involved combat (filters out NPC-vs-NPC and environmental damage)
  - DPS updates stop within 5-10 seconds after boss fights instead of continuing indefinitely
- Fixed Settings window minimize button not working
  - Minimize button now works in all windows (Settings, Charts, etc.)
  - MainView still minimizes to tray, other windows minimize to taskbar

[Detailed Release Notes](docs/RELEASE_NOTES_1.5.3.md)

---

## Version 1.5.2

**🐛 Critical Bug Fix:**
- Fixed race condition crash during back-to-back raids (ArgumentException in DataStorage)
- Added thread-safe locking to prevent dictionary modification during enumeration
- Eliminates crashes in high-activity scenarios (raids, World Boss Carnage)

[Detailed Release Notes](docs/RELEASE_NOTES_1.5.2.md)

---

## Version 1.5.1

**🐛 Critical Bug Fixes:**
- Fixed player list showing enemies/NPCs ("Player 75", etc.) - now filters by ProfessionID
- Fixed Enhanced Skill Breakdown not loading historical encounter data
- Fixed BattleLogRecorder state management preventing BSON file creation
- Fixed crash when clicking on non-player entities

**✨ New Features:**
- BSON-first historical data loading (automatic fallback to SQL)
- Data source indicator shows "BSON data" or "SQL (no BSON)" status
- Automatic BattleLog event aggregation for full skill statistics

[Detailed Release Notes](docs/RELEASE_NOTES_1.5.1.md)

---

## Version 1.5.0

**🎉 Major Feature - Enhanced Skill Breakdown:**
- Complete skill analysis system with Lucky Hits tracking (inspired by StarResonanceDps)
- Four summary stats cards: Damage Info, Critical Hits, Lucky Hits, Distribution
- Visual charts: Skill Distribution pie chart, Damage Type bar chart
- Detailed skill-by-skill breakdown table with all stats
- Dark-themed UI with scrollable content

**🐛 Critical Bug Fix:**
- Fixed meter stuck in [Last] state when starting new encounter after timeout - race condition in section creation resolved

**🔧 UI/UX Improvements:**
- Fixed ComboBox dropdown visibility (white text on dark background)
- Added ScrollViewer to Detailed Breakdown for better navigation
- Removed redundant "Skill Breakdown" tab (consolidated into "Detailed Breakdown")

[Detailed Release Notes](docs/RELEASE_NOTES_1.5.0.md)

---

## Version 1.4.7

**🐛 Critical Bug Fix:**
- Fixed meter stuck in "Last Battle" mode after manual reset - meter now properly clears last battle state

**🔧 Development (Module Solver - Disabled in UI):**
- Added OCR capture service for module detection
- Added network device selection for packet capture
- Added InverseBooleanConverter for UI bindings
- Module Solver remains disabled until fully tested

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.7.md)

---

## Version 1.4.6

**🐛 Critical Bug Fixes:**
- Fixed meter freeze after timeout - meter now properly accepts new data after manual reset
- Fixed plugin enable/disable logic - restored access to all plugins except Module Solver (temporarily disabled)

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.6.md)

---

## Version 1.4.5

**🎉 Major Features:**
- **Advanced Combat Logging System (Beta)** - Packet-level combat logging with BSON format
  - Full encounter replay capability
  - Rolling window storage (5/10/20/50 encounters)
  - Compatible with StarResonanceDps replay system
  - Zero performance impact when disabled (default)
- **Combat Logs Window** - Manage and view stored BSON encounter logs
- **Replay Window** - Timeline visualization and replay controls (UI ready)

**🐛 Bug Fixes:**
- Fixed double-click registration in daily/weekly task +/- buttons
- Fixed chart persistence race condition (charts disappearing after encounters)

**✨ Enhancements:**
- New Settings section for Advanced Combat Logging configuration
- History window shows BSON log availability per encounter
- Improved chart data synchronization between services

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.5.md)

---

## Version 1.4.4

**🐛 Critical Hotfix:**
- Fixed crash when interacting with damage meter after disabling mouse-through mode
- Fixed XAML binding errors on read-only tank stat properties (EffectiveDamage, MitigationPercent, EffectiveTps)
- Popup tooltips now display reliably without crashes

[Detailed Release Notes](docs/RELEASE_NOTES_1.4.4.md)

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
