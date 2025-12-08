# BlueMeter v1.5.1 Release Notes

## Overview
Version 1.5.1 fixes critical issues with historical data loading and player filtering in the Enhanced Skill Breakdown feature.

## Bug Fixes

### Fixed Player List Showing Enemies/NPCs
- **Issue**: Player dropdown showed "Player 75", "Player 76", etc. (enemies) alongside real players
- **Fix**: Added ProfessionID filtering to show only actual players
  - Filters both live mode and BSON historical data
  - Enemies/NPCs without ProfessionID are excluded
  - Prevents crashes when selecting non-player entities

### Fixed Historical Data Loading
- **Issue**: Enhanced Skill Breakdown didn't load historical encounter data
- **Implemented**: BSON-first loading strategy
  - Historical encounters automatically load from BSON files when available
  - Falls back to SQL database if BSON file not found
  - Live combat continues using SQL for real-time performance

### Fixed BSON Data Aggregation
- Added automatic BattleLog event aggregation for BSON data
- Calculates skill statistics from raw combat events:
  - Total damage and DPS per skill
  - Hit counts, crit rates, lucky hit rates
  - Min/Max damage tracking
  - Average damage per hit

### Fixed BattleLogRecorder State Management
- **Issue**: Recorder wasn't properly transitioning to Running state
- **Fix**: Added `State = RunningState.Running` in `Start()` method
- **Issue**: Empty string config for BattleLogDirectory caused initialization failure
- **Fix**: Convert empty strings to null for default path handling

## Improvements

### Data Source Indicator
- Added status indicator showing data source:
  - "✓ BSON data" when loading from BSON files
  - "📊 SQL (no BSON)" when using SQL fallback
- Removed manual toggle for cleaner UX
- Fully automatic selection based on data availability

### Enhanced Player Name Resolution
- Load player names from BSON PlayerInfos when available
- Fallback to EncounterData PlayerStats
- Better handling of missing player names

## Technical Details

### Modified Files
- `BlueMeter.Core/Data/BattleLogRecorder.cs`
  - Fixed state management in Start() method

- `BlueMeter.WPF/Services/ApplicationStartup.cs`
  - Fixed empty string handling for BattleLogDirectory

- `BlueMeter.WPF/ViewModels/EnhancedSkillBreakdownViewModel.cs`
  - Implemented BSON-first loading strategy
  - Added player filtering by ProfessionID
  - Added BattleLog event aggregation
  - Added data source tracking

- `BlueMeter.WPF/Views/EnhancedSkillBreakdownView.xaml`
  - Replaced toggle with data source indicator
  - Updated UI bindings

### Known Issues
- BSON file creation may still show "0 events" in some scenarios (under investigation)
- BattleLogRecorder event subscription needs further verification

## Installation
Download the latest release from the [Releases page](https://github.com/caaatto/BlueMeter/releases/tag/v1.5.1).

## Upgrade Notes
- No breaking changes
- Existing BSON files will be automatically detected and used
- SQL database continues to work as fallback
