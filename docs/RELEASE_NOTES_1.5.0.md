# BlueMeter v1.5.0 Release Notes

## 🎉 Major Feature: Enhanced Skill Breakdown with Lucky Hits Tracking

This release introduces a comprehensive skill breakdown system inspired by StarResonanceDps, bringing detailed combat analytics to BlueMeter's Advanced Combat Log window.

---

## ✨ New Features

### Enhanced Skill Breakdown Tab
A complete overhaul of skill analysis with StarResonanceDps-style statistics:

**📊 Four Summary Stats Cards:**
- **💥 Damage Info**: Total Damage, DPS, Hit Count
- **⚡ Critical Hits**: Crit Rate, Crit Count, Total Crit Damage
- **✨ Lucky Hits**: Lucky Rate, Lucky Count, Total Lucky Damage (NEW!)
- **📊 Distribution**: Normal Damage, Average Per Hit, Combat Duration

**📈 Visual Charts:**
- **🎯 Skill Distribution Pie Chart**: Top 10 skills by damage with color-coded breakdown
- **🔥 Damage Type Bar Chart**: Visual comparison of Normal, Critical, and Lucky damage

**⚔️ Detailed Skill Breakdown Table:**
- Complete skill-by-skill analysis with:
  - Skill Name (translated)
  - Total Damage
  - DPS
  - Hit Count
  - Crit Rate %
  - Crit Count
  - Lucky Rate % (NEW!)
  - Lucky Count (NEW!)
  - Average Damage Per Hit
  - Percentage of Total Damage

**🎨 Dark-Themed UI:**
- Professional dark color scheme matching BlueMeter's aesthetic
- Scrollable content for long skill lists
- Player selection dropdown with proper styling

---

## 🐛 Bug Fixes

### [Last Battle] State Stuck Bug (CRITICAL FIX)
**Problem**: When starting a new encounter after the meter entered [Last] state from a timeout, the meter would get stuck showing the previous battle time without resetting, even with hard reset.

**Root Cause**: Race condition where `NewSectionCreated` event fired twice:
1. Combat ends → enters [Last] state
2. Long gap passes (>15s timeout)
3. New encounter starts → `NewSectionCreated` fires
4. Data gets cleared
5. `NewSectionCreated` fires AGAIN (race condition)
6. Second call happens after data cleared but before new damage added
7. Meter checks for damage, finds none → stays stuck in [Last]

**Fix**: Added race condition detector in `DpsStatisticsViewModel.cs`:
```csharp
// If we detect new damage while awaiting section start but hasSectionDamage is false,
// this indicates a race condition. Clear [Last] state immediately to prevent stuck meter.
if (_awaitingSectionStart && hasNewDamage && !hasSectionDamage)
{
    _logger.LogWarning("[LAST BATTLE] Detected race condition. Clearing [Last] state.");
    _awaitingSectionStart = false;
    IsShowingLastBattle = false;
    BattleStatusLabel = string.Empty;
    _lastBattleDataSnapshot = null;
}
```

---

## 🔧 Improvements

### UI/UX Enhancements
- **ComboBox Styling**: Fixed white-on-white text visibility issue in dropdown menus
  - Dark background (#3F3F46) with white foreground
  - Blue highlight (#007ACC) on hover/selection
  - Consistent styling for both collapsed and expanded states

- **Scrollable Content**: Added ScrollViewer to Detailed Breakdown tab for better navigation of long skill lists

- **Tab Cleanup**: Removed redundant "Skill Breakdown" tab
  - "Detailed Breakdown" now provides all functionality in one comprehensive view
  - Cleaner tab structure with focused analytics

### Data Access Improvements
- Enhanced ViewModel to check both `ReadOnlyFullDpsDatas` and `ReadOnlySectionedDpsDataList`
- Auto-select first available player when opening the window
- Improved duration calculation using `LastLoggedTick - StartLoggedTick` (matches BlueMeter's standard approach)

---

## 📋 Technical Details

### New Files
- `BlueMeter.WPF/Views/EnhancedSkillBreakdownView.xaml` - Main UI layout
- `BlueMeter.WPF/Views/EnhancedSkillBreakdownView.xaml.cs` - Code-behind with DataContext injection
- `BlueMeter.WPF/ViewModels/EnhancedSkillBreakdownViewModel.cs` - ViewModel with stat calculations

### Modified Files
- `BlueMeter.WPF/ViewModels/DpsStatisticsViewModel.cs` - Race condition fix
- `BlueMeter.WPF/Views/ChartsWindow.xaml` - Tab structure update
- `BlueMeter.WPF/Views/ChartsWindow.xaml.cs` - View wiring and cleanup
- `BlueMeter.WPF/App.xaml.cs` - DI registration

### Architecture
- Uses MVVM pattern with CommunityToolkit.Mvvm
- OxyPlot integration for charts
- Real-time data binding with `IDataStorage`
- 1-second update timer for live stats

---

## 🎯 Breaking Changes
None - This is a feature-additive release.

---

## 📝 Notes
- Lucky Hits tracking requires game data to provide `LuckyTimes` in `SkillData`
- Charts automatically update every second during active combat
- Skill names are automatically translated using DeepL integration
- All stats cards and charts use the same dark theme as the main application

---

## 🙏 Credits
Enhanced Skill Breakdown design inspired by [StarResonanceDps](https://github.com/user/StarResonanceDps) repository.

---

**Full Changelog**: https://github.com/caaatto/BlueMeter/compare/v1.4.7...v1.5.0
