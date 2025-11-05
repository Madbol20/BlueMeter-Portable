# BlueMeter TODO List

## TODO List

### TODO

- [x] SettingsView -> Theme setting implementation
- [x] Comprehensive theme system with color filters
- [x] Live theme preview in settings
- [x] Background image support with full opacity
- [x] Theme color overlay on background images
- [x] Semi-transparent UI panels for contrast
- [x] MessageView -> Consider refactoring to MVVM (if needed)
- [x] SettingsView (VM) -> Review the i18n usage in Cancel() for correctness
- [x] Player information update
- [x] Combat information not refreshing
- [ ] Healing skill list is incorrect
- [x] Fix TryDetect Server when there is no process running
- [x] RegisterHotKey failed for Topmost: F7+Control
- [ ] Simplify logging
- [ ] Logging detail for syncing
- [ ] Optimize nuget references
- [ ] i18n fallback
- [ ] DPS Statistics View -> Binding error
- [ ] Export DPS statistics image
- [ ] Not to update port filter if the ports are same

### Issues

- [ ] Incorrect detection of the player's class
- [ ] (WPF) Refresh promptly after retrieving cached user information
- [ ] (WPF) Manual TopMost toggle sometimes fails to clear

### Features

- [ ] Local data caching feature
- [ ] (WinForm) Attempt GPU-accelerated control rendering
- [ ] Allow launching with Shift/Ctrl held to reset user settings

### Remaining issues to be examined

- [ ] Display team total damage in DPS statistics
- [ ] Add scrollbar to DPS statistics
- [ ] Add training dummy selection in dummy mode (select rightmost or NPC-behind dummy to avoid debuff stacking or damage interference from other players)
- [ ] Add NPC data
- [ ] Add level and armband level to data collection

### CheckList

- [ ] Synchronize window transparency with mouse-through; window should be transparent only when mouse-through is enabled
