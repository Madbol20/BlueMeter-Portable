# Release Notes - Version 1.5.3

**Release Date:** TBD

## Bug Fixes

### 🐛 Fixed DPS Meter Continuing After Combat Ends

**Issue:** DPS meter would continue calculating for 10+ seconds after a boss fight ended, even when players were in inventory or traveling to the next raid.

**Root Cause:** The meter was tracking ALL damage events, including:
- NPC-vs-NPC combat in the boss lobby
- Environmental damage effects
- Other non-player-involved damage sources

These lingering damage packets would reset the combat timeout, keeping the meter "active" even though the actual player combat had ended.

**User Report:** "after bone I went into my inventory, then to the light raid and during all this time in my inventory and traveling to the next raid it kept going"

**Fix Applied:**
- Added filter in `DeltaInfoProcessors.cs` to only log damage where at least one participant is a player
- Combat now only tracks when:
  - Players deal damage to enemies ✅
  - Players take damage from enemies ✅
  - Players heal or are healed ✅
  - Players engage in PvP ✅
- Combat now ignores:
  - NPC-vs-NPC fights ❌
  - Environmental damage to NPCs ❌
  - Other non-player-involved events ❌

**Impact:**
- DPS meter now stops updating much faster after combat ends (typically within 5-10 seconds)
- Eliminates false combat detection from environmental effects in boss lobbies
- If the current player dies but party members are still fighting, tracking continues (since other players are involved)
- The existing 15-second timeout for creating new sections remains unchanged

### 🐛 Fixed Settings Window Minimize Button Not Working

**Issue:** Clicking the minimize button in the Settings window had no effect, even though the button was visible.

**Root Cause:** The Header control's minimize button was only bound to a command (`MinimizeToTrayCommand`), but the SettingsView didn't provide this command. MainView provides the command to minimize to tray, but Settings window didn't have any minimize functionality.

**Fix Applied:**
- Added click event handler in Header control as fallback when no command is bound
- MainView continues to minimize to system tray (existing behavior)
- Settings and other windows now minimize to taskbar when clicking the button

**Impact:**
- Minimize button now works in all windows (Settings, Charts, etc.)
- MainView still minimizes to tray as before
- Other windows minimize to taskbar normally

## Files Changed

### Modified
- `BlueMeter.Core/Analyze/V2/Processors/DeltaInfoProcessors.cs`
  - Added filter at lines 120-125 to skip non-player combat events
  - Only logs battle logs where `isAttackerPlayer || isTargetPlayer` is true

- `BlueMeter.WPF/Controls/Header.xaml`
  - Added Click event handler to minimize button (line 70)
  - Updated tooltip from "Minimize to Tray" to "Minimize" for generic windows

- `BlueMeter.WPF/Controls/Header.xaml.cs`
  - Added `MinimizeButton_Click` handler (lines 49-61)
  - Minimizes window to taskbar when no command is bound
  - Preserves command behavior when MinimizeToTrayCommand is provided

## Testing Notes

**DPS Meter Fix:**
1. Complete a boss fight in a raid
2. Open inventory or start traveling immediately after boss death
3. Observe that DPS updates stop within 5-10 seconds (instead of continuing indefinitely)
4. Verify that combat tracking continues normally during actual player combat

**Minimize Button Fix:**
1. Open Settings window
2. Click the minimize button (−) in the top right
3. Window should minimize to taskbar
4. Restore from taskbar and verify window state is normal

## Known Issues

None introduced by this release.

## Upgrade Notes

This is a bugfix release. Simply replace your existing BlueMeter installation with v1.5.3.

---

**Special Thanks:**
- Community member "Bella" for the detailed bug report
