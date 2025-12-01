# BlueMeter v1.4.0 Release Notes

## 🎯 New Features

### Tank/Mitigation Statistics

**Overview**: Comprehensive tank performance metrics for tracking defensive performance and threat generation.

**New Metrics**:
- **HP Damage Taken**: Actual health damage received
- **Shield Damage Absorbed**: Damage absorbed by shields and barriers
- **Total Effective Damage**: Combined damage for threat tracking
- **Mitigation Percentage**: Effectiveness of damage reduction
- **Effective TPS (Threat Per Second)**: Real-time threat generation rate

**Use Cases**:
- Analyze tank survivability and mitigation effectiveness
- Track shield uptime and absorption rates
- Optimize threat generation for better aggro control
- Compare different tank builds and gear setups

---

### Queue Pop Alerts (Beta)

**Overview**: Audio notifications when dungeon/raid queue pops, even when the game is minimized.

**Features**:
- **OCR-based Detection**: Analyzes game window to detect queue pop UI
- **Works When Minimized**: Get alerts even if the game is in the background
- **Customizable Alert Sounds**: Multiple sound options to choose from
- **Configurable Settings**: Enable/disable and customize in Settings → Alerts (Beta)

**How It Works**:
1. Captures the bottom 70% of game window (where player cards appear)
2. Runs OCR to detect player card patterns (Lv.XX indicators)
3. Counts player cards and checks for blacklisted words
4. Triggers audio alert when 3+ player cards detected
5. 10-second cooldown prevents duplicate alerts

**Configuration**:
- Navigate to Settings → Alerts (Beta)
- Enable Queue Pop Alerts
- Select your preferred alert sound
- Adjust volume as needed

---

## 🔧 Improvements

### Code Quality
- **Removed deprecated WinForms dependencies**: Fully migrated to WPF
- **Cleaned up alert system logging**: Reduced console spam for cleaner logs
- **Code cleanup and optimization**: Improved performance and maintainability

---

## 🐛 Bug Fixes

### Damage Tracking Fix
- **Fixed damage tracking to properly separate HP damage from shield absorption**
- HP damage and shield absorption are now tracked separately
- Enables accurate mitigation percentage calculations
- Improves threat calculation accuracy

---

## 📦 Installation

Download the latest release from the [Releases page](https://github.com/caaatto/BlueMeter/releases) and follow the installation instructions in the README.

---

## 🎯 What's New Summary

- ✅ Tank statistics with HP damage, shield absorption, and TPS
- ✅ Queue pop alerts with OCR detection
- ✅ Customizable alert sounds
- ✅ Works when game is minimized
- ✅ Cleaner codebase without WinForms dependencies
- ✅ Accurate damage tracking with HP/Shield separation

---

**Thank you for using BlueMeter!** 🎉
