# BlueMeter v1.4.1 Release Notes

## 🐛 Hotfix

### Missing OCR Data File

**Issue**: Queue Pop Alerts feature was not working in Release builds due to a missing Tesseract OCR data file.

**Problem**:
- The `eng.traineddata` file required by Tesseract OCR was not being included in Release builds
- This caused the Queue Pop Alerts (Beta) feature to fail when users ran the Release version
- Debug builds worked correctly because they had the file in a different location

**Fix Applied**:
- Added `eng.traineddata` to the build output
- Ensured the file is correctly copied to the Release build directory
- Updated build configuration to prevent this issue in future releases

**Result**:
- ✅ Queue Pop Alerts now work correctly in all build configurations
- ✅ Users can enable and use the feature in Settings → Alerts (Beta)
- ✅ OCR detection functions properly for queue pop detection

---

## 🎯 What's Fixed

- ✅ Queue Pop Alerts (Beta) now working in Release builds
- ✅ Tesseract OCR data file included in all builds
- ✅ No more missing file errors

---

## 📦 Installation

Download the latest release from the [Releases page](https://github.com/caaatto/BlueMeter/releases) and follow the installation instructions in the README.

---

**Thank you for using BlueMeter!** 🎉
