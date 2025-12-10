# Release Notes - Version 1.5.3

**Release Date:** TBD

## Bug Fix

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

## Files Changed

### Modified
- `BlueMeter.Core/Analyze/V2/Processors/DeltaInfoProcessors.cs`
  - Added filter at lines 120-125 to skip non-player combat events
  - Only logs battle logs where `isAttackerPlayer || isTargetPlayer` is true

## Testing Notes

To verify this fix:
1. Complete a boss fight in a raid
2. Open inventory or start traveling immediately after boss death
3. Observe that DPS updates stop within 5-10 seconds (instead of continuing indefinitely)
4. Verify that combat tracking continues normally during actual player combat

## Known Issues

None introduced by this release.

## Upgrade Notes

This is a bugfix release. Simply replace your existing BlueMeter installation with v1.5.3.

---

**Special Thanks:**
- Community member "Bella" for the detailed bug report
