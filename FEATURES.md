# BlueMeter Features Guide

A comprehensive guide to all features available in BlueMeter, a DPS analysis tool for Star Resonance.

---

## Core Features

### 1. DPS Statistics & Real-Time Tracking

**What it does:** Displays live damage and healing statistics for all players in combat.

**Features:**
- Real-time DPS (Damage Per Second) tracking
- Healing per second (HPS) tracking
- Damage taken statistics
- Critical hit count and rate
- Individual player breakdowns
- Party contribution percentages

**How to use:**
- Launch BlueMeter and start playing
- Statistics automatically appear when combat begins
- Click on any player to view detailed skill breakdown

---

### 2. Solo Training Mode

**What it does:** Filters DPS statistics to show only specific players, perfect for practice sessions.

**Two Modes:**
- **Personal Training** - Shows only YOUR damage (filters to current player)
- **Group Training** - Shows only YOUR party/group members

**How to use:**
1. Open Settings
2. Find "Training Mode" section
3. Select "Personal Training" or "Group Training"
4. Optionally set Manual UID override if auto-detection fails
5. Mode resets on app restart for safety

**Use cases:**
- Solo practice sessions (seeing only your performance)
- Party-only tracking (excluding other nearby players)
- Accurate personal DPS measurement

---

### 3. Recording & Encounter History

**What it does:** Automatically saves combat encounters to review later.

**Recording Options:**
- **Record All Encounters** - Save every fight regardless of length
- **Duration Filtering:**
  - Ignore fights under 1 minute
  - Ignore fights under 2 minutes
  - Set custom minimum duration

**Automatic Cleanup:**
- Auto-cleanup enabled by default
- Keep last 20 encounters (configurable)
- Maximum database size: 100 MB (configurable)

**How to use:**
1. Configure recording settings in Settings window
2. Encounters automatically save after combat ends
3. Open "Encounter History" to browse past fights
4. Click any encounter to load and review detailed statistics
5. Return to live mode with the "Back to Live" button

**Stored Data:**
- Start time and duration
- All player statistics
- Per-skill damage breakdowns
- DPS/HPS over time (chart data)

---

### 4. Queue Pop Alerts

**What it does:** Plays a sound notification when your dungeon/raid queue pops.

**Alert Options:**
- Choose from 6 different sounds (Drum, Harp, Wow, Yoooo, DungeonFound, QPop)
- Adjustable volume (0-100%)
- Enable/disable toggle

**Detection Methods:**
1. **Network-based** (primary) - Detects queue pop from game packets
2. **OCR-based** (fallback) - Uses screen capture and text recognition
   - Works even when game is minimized
   - Supports multiple languages (English, German, Japanese, Chinese, French)
   - 100% local processing (no external servers)
   - 10-second cooldown to prevent spam

**How to use:**
1. Open Settings → Queue Alerts
2. Enable queue alerts
3. Select your preferred sound
4. Adjust volume to taste
5. Test with the "Test Sound" button

---

### 5. Charts & Advanced Analytics

**What it does:** Visual representation of combat data with interactive charts.

**Available Charts:**

**DPS Trend Chart:**
- Real-time line graph showing DPS over time
- Multi-player tracking (up to 8 players with distinct colors)
- Default: Last 60 seconds of combat
- Updates every 500ms

**Skill Breakdown Chart:**
- Pie chart showing damage distribution by skill
- Top 10 skills displayed
- Color-coded segments
- Single player view with player selector

**How to use:**
1. Click "Charts" or "Advanced" button
2. Select chart type from tabs
3. Choose player from dropdown (Skill Breakdown only)
4. Toggle auto-refresh on/off
5. Works with both live and historical encounters

---

### 6. Skill Breakdown View

**What it does:** Detailed per-skill statistics for any player.

**Statistics Shown:**
- Total damage and DPS per skill
- Hit count (total attacks)
- Critical hit count and rate
- Average/Min/Max damage per skill
- Normal vs critical damage split

**Features:**
- Automatic skill name translation
- Supports historical encounter data
- Click any player in main window to view their breakdown

---

### 7. Customization & Display Settings

**What it does:** Extensive visual customization options.

**Theme Options:**
- **Theme Mode:** Light or Dark
- **Panel Color:** Light (white) or Dark (gray)
- **Custom Theme Color:** Choose any color via hex picker
- **Background Image:** Set custom background image
- **Background Only Mode:** Hide panels, show only background
- **Opacity:** Adjust window transparency (0-100%)

**How to use:**
1. Open Settings → Appearance
2. Experiment with different combinations
3. Changes apply instantly (no restart needed)

---

### 8. Overlay & Window Management

**What it does:** Control how BlueMeter windows behave on screen.

**Features:**
- **Mouse Through (F6):** Click-through mode - clicks pass through to game
- **Always On Top (F7):** Keep window on top of all others
- **Minimize to Tray:** Hide to system tray instead of taskbar
- **Auto-save Position:** Window positions saved on close
- **Multiple Windows:** Separate windows for stats, charts, history, settings

**Keyboard Shortcuts:**
- **F6:** Toggle Mouse Through
- **F7:** Toggle Always On Top
- **F9:** Clear current combat data
- All shortcuts customizable in Settings

---

### 9. Language Support

**What it does:** Multi-language interface with instant switching.

**Available Languages:**
- Auto (system default)
- English
- Chinese (Simplified)
- Portuguese (Brazilian)

**How to use:**
1. Open Settings → Language
2. Select preferred language
3. UI updates instantly (no restart required)

---

### 10. Network Adapter Selection

**What it does:** Choose which network adapter to monitor for game traffic.

**Options:**
- Auto-select (recommended)
- Manual adapter selection
- Monitors network changes automatically

**How to use:**
1. Open Settings → Network
2. Select preferred adapter or use auto-select
3. Restart BlueMeter if changed mid-session

---

### 11. Checklist & Task Tracking

**What it does:** Track daily and weekly in-game tasks.

**Features:**
- Daily task list with midnight reset
- Weekly task list with Monday reset
- Countdown timers for resets
- Event timers for active events
- Show/hide completed tasks
- Search/filter tasks by name
- Multiple profile support

**How to use:**
1. Open Checklist window
2. Check off tasks as you complete them
3. View reset timers to plan your schedule
4. Tasks auto-reset at configured times

---

### 12. Plugins System

**What it does:** Modular feature system for extended functionality.

**Available Plugins:**

**DPS Tool Plugin:**
- Main DPS statistics window
- Real-time damage tracking
- Core BlueMeter functionality

**World Boss Plugin:**
- Boss tracking and scheduling
- World event monitoring
- Boss timer notifications

**Module Solver Plugin:**
- Interactive puzzle/module solver
- In-game challenge assistance

**How to use:**
1. Open Settings → Plugins
2. Enable/disable plugins as needed
3. Configure auto-start for each plugin
4. Plugin settings saved automatically

---

### 13. Encounter History Browser

**What it does:** Browse and review past combat encounters.

**Features:**
- Lists up to 100 recent encounters
- Shows start time, duration, and participants
- Load any encounter to review full statistics
- View historical chart data
- Works with all analysis features (charts, skill breakdown, etc.)

**How to use:**
1. Click "Encounter History" button
2. Browse list of past fights
3. Click any encounter to load
4. Use charts and breakdown views normally
5. Click "Back to Live" to return to real-time mode

---

### 14. Debug & Logging Tools

**What it does:** Advanced troubleshooting and diagnostics.

**Features:**
- Real-time log viewer with auto-scroll
- Filter logs by text content
- Select log level (Trace, Debug, Info, Warning, Error, Critical)
- Shows last 2000 log entries
- Log count and filter result count
- Queue detection special logging mode

**How to use:**
1. Enable "Debug Mode" in Settings
2. Open Debug window
3. Use filter box to search logs
4. Select minimum log level to display
5. Check "Auto-scroll" to follow latest logs

---

## Keyboard Shortcuts Reference

| Shortcut | Action | Customizable |
|----------|--------|--------------|
| **F6** | Toggle Mouse Through (click-through) | Yes |
| **F7** | Toggle Always On Top | Yes |
| **F9** | Clear current combat data | Yes |

All shortcuts can be customized in Settings → Hotkeys. Delete key clears a shortcut.

---

## Configuration Files

**Location:** `BlueMeter.WPF/bin/Release/net8.0-windows/`

- `appsettings.json` - Main application configuration
- `EncounterHistory.db` - SQLite database for saved encounters
- `logs/` - Application logs for troubleshooting

---

## Tips & Best Practices

### For Accurate DPS Tracking:
- Use Solo Training Mode when practicing alone
- Set minimum encounter duration to filter out trash fights
- Check Skill Breakdown to identify your top damage sources

### For Performance:
- Disable auto-cleanup if you want to keep all history
- Reduce chart update frequency if experiencing lag
- Use Background Only Mode for minimal UI

### For Queue Alerts:
- Enable queue detection logging if alerts aren't working
- Test different sounds to find one you'll notice
- OCR detection works even when alt-tabbed

### For Window Management:
- Use Mouse Through + Always On Top for perfect overlay
- Customize opacity to see game clearly
- Save window positions by closing normally (not killing process)

---

## Troubleshooting

**Queue alerts not working?**
- Enable "Queue Detection Logging" in Settings
- Check Debug window for detection messages
- Ensure game process is running (star, BPSR_STEAM, BPSR_EPIC, BPSR)

**DPS not showing?**
- Verify network adapter is correct in Settings
- Check that you're in active combat
- Try manual adapter selection

**Settings not saving?**
- Close BlueMeter normally (don't kill process)
- Check that `appsettings.json` isn't read-only
- Check logs for save errors

**Translation issues?**
- Some skill names may still be in Chinese
- Report issues on GitHub for translation updates

---

## Getting Help

- **Logs:** Check `logs/all-messages.log` for errors
- **GitHub Issues:** https://github.com/caaatto/BlueMeter/issues
- **Debug Mode:** Enable in Settings to see detailed diagnostics

---

## Credits

Based on the original StarResonanceDpsAnalysis project, translated and enhanced for the international community.

**Technology:**
- WPF .NET 8.0
- Network packet capture (Npcap)
- Tesseract OCR for queue detection
- SQLite for encounter storage
- MVVM architecture

**License:** AGPL v3
