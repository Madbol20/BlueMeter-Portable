# Release Notes - Version 1.5.3

**Release Date:** TBD

## New Features

### 🎨 Dynamic Complementary Button Colors

**Description:** Launch button and Daily/Weekly Checklist button now feature dynamic gradient colors that automatically adapt to the user's selected theme color.

**Implementation:**
- Created `ComplementaryColorGradientConverter` that calculates the complementary color (180° opposite on the color wheel)
- Converts RGB to HSL, rotates hue by 180°, and creates a gradient with two shades
- Buttons automatically update when theme color changes
- Provides visual consistency while maintaining user color preferences

**User Impact:**
- More cohesive visual design
- Buttons stand out with complementary colors
- Automatic adaptation to any theme color selection

### 🎄 Christmas Theme Decorations

**Description:** Complete holiday theme system with festive Christmas decorations for BlueMeter.

**Features:**
- **Santa Hat** - Festive decoration positioned above the Launch button in MainView
  - 15° rotation for natural appearance
  - Semi-transparent (95% opacity) to blend with UI
- **Snowfall Effects** - Animated snowflakes falling across the entire window
  - Full-height coverage from top to bottom
  - Randomized sizes, speeds, and horizontal drift
  - Smooth fade-in/fade-out animations
- **Frost Border** - Decorative frost frame around the DPS meter window
  - Scaled to extend beyond meter edges
  - Semi-transparent overlay effect
- **Christmas Lights** - String light decorations
- **Christmas Bell** - Interactive bell with music playback
  - Click to play festive instrumental music (15% volume)
  - 95% chance: Jingle Bells instrumental
  - 5% chance: Carol of Bells instrumental (surprise Easter egg!)
  - Visual feedback: Bell swings when clicked
- **Candy Cane Cursor** - Custom festive cursor
  - Appears when holiday themes are enabled
  - Tilted 15° to the left for natural appearance
  - Automatically reverts to default cursor when themes disabled
- **Dynamic Window Title** - Shows event name when themes enabled
  - Example: "BlueMeter - Christmas 🎄"
  - Automatically updates based on active holiday
- **User Color Preservation** - Holiday themes only add decorations
  - User's selected theme color remains unchanged
  - No color override when holiday themes are enabled

**Technical Implementation:**
- Created `HolidayTitleConverter.cs` for dynamic window title formatting
- Modified `AppConfig.cs` to preserve user color settings during holidays
- Added `MainSnowCanvas` to root Grid for full-height snow coverage
- Optimized snowflake fade-in animation (0.3s instead of 2s) for proper visibility
- Used `RenderTransform` with `ScaleTransform` for frost border positioning
- Santa Hat positioned with overflow decoration using `Panel.ZIndex`
- Created `CursorHelper.cs` for custom cursor creation from PNG images with rotation support
- Bell uses `MediaPlayer` for MP3 playback with 15% volume and random track selection
- Music files stored in `Assets/Themes/Christmas/music/`
- Candy cane cursor created with P/Invoke to Windows API (CreateIconIndirect)

**User Impact:**
- Festive visual experience during holiday seasons
- No interference with functionality or user preferences
- Can be toggled on/off in Settings → Advanced → Holiday Themes
- All decorations are non-intrusive and maintain UI usability

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

**Issue #1:** Clicking the minimize button in the Settings window had no effect, even though the button was visible.

**Root Cause:** The Header control's minimize button was only bound to a command (`MinimizeToTrayCommand`), but the SettingsView didn't provide this command. MainView provides the command to minimize to tray, but Settings window didn't have any minimize functionality.

**Issue #2:** After minimizing Settings window, clicking "Settings" in MainView or meter didn't restore the window from minimized state.

**Root Cause:** The `Show()` method doesn't automatically restore a minimized window - it needs explicit WindowState management.

**Fix Applied:**
- Added click event handler in Header control as fallback when no command is bound
- Overrode `Show()` method in SettingsView to restore from minimized state before showing
- MainView continues to minimize to system tray (existing behavior)
- Settings and other windows now minimize to taskbar when clicking the button

**Impact:**
- Minimize button now works in all windows (Settings, Charts, etc.)
- MainView still minimizes to tray as before
- Other windows minimize to taskbar normally
- Settings window properly restores from minimized state when reopened

## Files Changed

### Added
- `BlueMeter.WPF/Converters/HolidayTitleConverter.cs`
  - New converter for combining app name with holiday name in window titles
  - Takes AppConfig as input and returns formatted title string

- `BlueMeter.WPF/Converters/ComplementaryColorGradientConverter.cs`
  - Calculates complementary color from theme color (180° hue rotation)
  - Creates LinearGradientBrush with two shades for visual depth
  - Converts between RGB and HSL color spaces

- `BlueMeter.WPF/Helpers/CursorHelper.cs`
  - Creates custom cursors from PNG images using Windows API
  - Supports image rotation for custom cursor angles
  - Uses P/Invoke (CreateIconIndirect, GetIconInfo) for cursor creation

### Modified
- `BlueMeter.WPF/Config/AppConfig.cs`
  - Modified `GetEffectiveThemeColor()` to always return user's selected color
  - Added `GetCurrentHolidayName()` method and `CurrentHolidayName` property
  - Holiday themes now only add decorations without changing colors

- `BlueMeter.WPF/Converters/ConveterDictionary.xaml`
  - Added HolidayTitleConverter to global resources (line 34)
  - Added ComplementaryColorGradientConverter to global resources (line 35)

- `BlueMeter.WPF/Views/MainView.xaml`
  - Added Title binding with HolidayTitleConverter (line 14)
  - Created MainSnowCanvas in root Grid for full-height snow coverage (lines 328-338)
  - Added Santa Hat decoration above Launch button (lines 919-934)
  - Updated PluginRunButton style to use ComplementaryColorGradientConverter (line 293)

- `BlueMeter.WPF/Views/MainView.xaml.cs`
  - Added custom cursor support with UpdateChristmasCursor() method
  - Subscribes to AppConfig.PropertyChanged to toggle cursor
  - Creates candy cane cursor with 15° left tilt when holiday themes enabled

- `BlueMeter.WPF/Views/DpsStatisticsView.xaml`
  - Updated to use HolidayTitleConverter for window title (line 18)

- `BlueMeter.WPF/Controls/ChristmasDecorations.xaml`
  - Modified SnowCanvas to bind to parent Grid dimensions
  - Removed Santa Hat (moved to MainView for better positioning)
  - Added clickable Christmas bell with MouseLeftButtonDown event
  - Removed IsHitTestVisible="False" from UserControl to enable bell clicks
  - Added IsHitTestVisible="False" to SnowCanvas and ChristmasLights individually

- `BlueMeter.WPF/Controls/ChristmasDecorations.xaml.cs`
  - Modified `CreateSnowflake()` to use MainSnowCanvas from parent window
  - Changed snowflake start position from -50 to -200
  - Optimized fade-in animation from 2 seconds to 0.3 seconds using keyframes
  - Added `FindVisualChild<T>()` helper for visual tree navigation
  - Added `ChristmasBell_Click()` event handler for music playback
  - Added `TriggerBellRing()` for visual bell swing animation
  - Implements 95/5 random selection between two Christmas music tracks
  - MediaPlayer volume set to 15% (0.15)
  - Uses UI thread dispatcher for MediaPlayer reliability

- `BlueMeter.WPF/Controls/DpsMeterChristmasDecorations.xaml`
  - Adjusted frost border RenderTransform scaling (lines 42-58)
  - ScaleX="1.3" ScaleY="1.5" with RenderTransformOrigin="0.5,0.3"
  - Removed snow texture overlay

- `BlueMeter.WPF/BlueMeter.WPF.csproj`
  - Added System.Drawing.Common package reference for cursor creation
  - Added Content entry for Assets\Themes\Christmas\music\*.mp3 files
  - Added Content entry for candy_cane.png (in addition to existing Resource entry)

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

- `BlueMeter.WPF/Views/SettingsView.xaml.cs`
  - Overrode `Show()` method (lines 345-355)
  - Restores window from minimized state before showing
  - Always activates and brings window to front

## Testing Notes

**Christmas Theme:**
1. Enable Holiday Themes in Settings → Advanced
2. Verify window title shows "BlueMeter - Christmas 🎄" (or current holiday name)
3. Check that Santa Hat appears above Launch button in MainView
4. Observe snowfall covering entire window from top to bottom
5. Verify snowflakes fade in quickly and are visible immediately
6. Check frost border around DPS meter extends beyond edges
7. Confirm user's selected theme color remains unchanged
8. Click the Christmas bell in bottom-right corner - music should play at low volume
9. Click bell multiple times to test random track selection (95% Jingle Bells, 5% Carol of Bells)
10. Verify candy cane cursor appears when hovering over MainView (tilted left)
11. Disable Holiday Themes and verify all decorations disappear and cursor reverts to normal

**Dynamic Button Colors:**
1. Change theme color in Settings → Appearance
2. Verify Launch button and Daily/Weekly Checklist button update with complementary gradient
3. Test with multiple different theme colors (blue, purple, green, etc.)
4. Confirm gradient automatically adapts to each color

**DPS Meter Fix:**
1. Complete a boss fight in a raid
2. Open inventory or start traveling immediately after boss death
3. Observe that DPS updates stop within 5-10 seconds (instead of continuing indefinitely)
4. Verify that combat tracking continues normally during actual player combat

**Minimize Button Fix:**
1. Open Settings window
2. Click the minimize button (−) in the top right
3. Window should minimize to taskbar
4. Click "Settings" in MainView or meter
5. Window should restore from minimized state and come to front
6. Verify window state is normal and all settings are visible

## Known Issues

None introduced by this release.

## Upgrade Notes

This is a bugfix release. Simply replace your existing BlueMeter installation with v1.5.3.

---

**Special Thanks:**
- Community member "Bella" for the detailed bug report
