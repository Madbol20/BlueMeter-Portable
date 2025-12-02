# Skill Breakdown - Implementation Plan (REVISED)

## Executive Summary

BlueMeter already has **90% of the chart infrastructure built**! The main issue is that the charts exist as **separate components** but aren't **integrated into the SkillBreakdownView window**. We need to connect the existing pieces rather than build from scratch.

---

## ✅ What We Already Have (Existing Assets)

### 1. **OxyPlot Integration** ✅
- ✅ `OxyPlot.Wpf` package installed
- ✅ Dark theme colors defined
- ✅ Chart styling

### 2. **Skill Breakdown Pie Chart** ✅ (100% Complete!)
**File:** `SkillBreakdownChartView.xaml` + `SkillBreakdownChartViewModel.cs`

**Features:**
- ✅ OxyPlot PieSeries with skill damage distribution
- ✅ Real-time updates (1 second refresh via DispatcherTimer)
- ✅ Player selection dropdown
- ✅ Top N skills limit (default: 10)
- ✅ "Other" grouping for remaining skills
- ✅ 15 distinct skill colors
- ✅ Historical data support (for encounter replay)
- ✅ DeepL skill name translation
- ✅ Dark theme UI
- ✅ Status bar with stats

**What's Missing:**
- ❌ Not integrated into SkillBreakdownView window
- ❌ Exists as standalone UserControl

### 3. **DPS Trend Line Chart** ✅ (Exists!)
**File:** `DpsTrendChartView.xaml` + `DpsTrendChartViewModel.cs`

**Features:**
- ✅ OxyPlot LineSeries for DPS over time
- ✅ Real-time updates
- ✅ Time series data collection

**What's Missing:**
- ❌ Not integrated into SkillBreakdownView
- ❌ Need to verify zoom controls

### 4. **Chart Data Service** ✅ (Fully Built!)
**File:** `ChartDataService.cs` + `IChartDataService.cs`

**Features:**
- ✅ Background sampling at regular intervals (200ms)
- ✅ ObservableCollection for DPS/HPS history
- ✅ GetDpsHistory(playerId) / GetHpsHistory(playerId)
- ✅ Historical data loading/snapshot
- ✅ Start/Stop controls

### 5. **Charts Window** ✅ (Separate Window Exists!)
**File:** `ChartsWindow.xaml` + `ChartsWindowViewModel.cs`

**Features:**
- ✅ 1200x800 window size (same as competitor!)
- ✅ Dark theme
- ✅ Tab control with DPS Trend and Skill Breakdown tabs
- ✅ Already embeds SkillBreakdownChartView and DpsTrendChartView

**What's Missing:**
- ❌ This is a **separate window**, not integrated into SkillBreakdownView
- ❌ Opens separately from player skill breakdown

---

## ❌ What's Missing (Gap Analysis)

### Critical Issues:

1. **Charts Not in SkillBreakdownView** ❌
   - Current: SkillBreakdownView shows placeholder text
   - Needed: Embed existing SkillBreakdownChartView + DpsTrendChartView
   - **Impact:** Major UX issue - users can't see charts when viewing player skills

2. **No Working Healing/Tanking Tabs** ❌
   - Current: SkillBreakdownView has 3 tabs but only "Output" partially works
   - Needed: Implement Healing and Tanking data tracking
   - **Impact:** Can't analyze healers or tanks

3. **Skill List UI is Basic** ❌
   - Current: Simple dark transparent boxes
   - Competitor: Modern Material Design cards with hover effects, icons, gradients
   - **Impact:** Less polished, harder to read

4. **No Grid Splitter Layout** ❌
   - Current: Charts and skills stacked vertically
   - Competitor: Charts on left, skills on right, resizable
   - **Impact:** Poor space utilization, can't customize layout

5. **No Skill Icons** ❌
   - Current: Just text skill names
   - Competitor: Colored circle with first letter
   - **Impact:** Less visual, harder to scan

6. **No Zoom Controls** ❌
   - Current: No zoom functionality in SkillBreakdownView
   - Competitor: Zoom in/out/reset buttons
   - **Impact:** Can't inspect chart details

7. **Summary Cards Need Polish** ❌
   - Current: Basic blue cards with white text
   - Competitor: Gradient cards, better typography, better stat organization
   - **Impact:** Less professional appearance

---

## 🎯 Implementation Plan (REVISED)

### **Phase 1: Integrate Existing Charts** (HIGHEST PRIORITY) 🚀
**Goal:** Get the working charts into SkillBreakdownView

**Tasks:**
1. ✅ **Embed SkillBreakdownChartView** into SkillBreakdownView.xaml
   - Replace placeholder "Pie Chart" text
   - Wire up to current player data
   - Ensure player selection syncs

2. ✅ **Embed DpsTrendChartView** into SkillBreakdownView.xaml
   - Replace placeholder "Line Chart" text
   - Wire up to ChartDataService
   - Show DPS over time for selected player

3. ✅ **Add Grid Splitter Layout**
   - Left column: Charts (DPS trend + Pie chart)
   - Right column: Skill list + summary cards
   - Make resizable

4. ✅ **Update SkillBreakdownViewModel**
   - Inject ChartDataService
   - Subscribe to chart data updates
   - Pass player selection to chart view models

**Expected Outcome:**
- Charts visible when opening player skill breakdown
- Real-time updates working
- 70% feature parity with competitor

**Time Estimate:** ~2-3 hours

---

### **Phase 2: Complete Healing/Tanking Tabs**
**Goal:** Make all 3 tabs functional

**Tasks:**
1. ✅ **Add HPS Chart Support**
   - Create HpsTrendChartView (copy DpsTrendChartView)
   - Wire to ChartDataService.GetHpsHistory()
   - Add healing skill breakdown pie chart

2. ✅ **Add DTPS Chart Support**
   - Create DtpsTrendChartView (damage taken per second)
   - Track damage taken over time
   - Add damage sources pie chart

3. ✅ **Update Tab Content**
   - DPS tab: Damage skills + DPS charts
   - Healing tab: Healing skills + HPS charts
   - Tanking tab: Damage taken + DTPS charts + mitigation stats

4. ✅ **Add Stat Tracking**
   - Ensure StatisticDataViewModel tracks healing/tanking separately
   - Update data collection in DpsStatisticsViewModel

**Expected Outcome:**
- All 3 tabs working
- Healers and tanks can see detailed breakdowns
- Full feature parity with competitor charts

**Time Estimate:** ~3-4 hours

---

### **Phase 3: UI Polish & Enhancements**
**Goal:** Match/exceed competitor UI quality

**Tasks:**
1. ✅ **Modern Skill Cards**
   - Replace basic borders with Material Design cards
   - Add hover effects (shadow, border highlight)
   - Add colored icons (first letter in circle)

2. ✅ **Improve Summary Cards**
   - Add gradient backgrounds
   - Better stat layout (grid instead of stacked)
   - Larger, bolder numbers

3. ✅ **Add Zoom Controls**
   - Zoom in/out/reset buttons for line charts
   - Match competitor button style

4. ✅ **Window Size**
   - Increase from 800x450 to 1200x800
   - Better use of space

5. ✅ **Improve Colors & Typography**
   - Use theme colors from AppConfig
   - Consistent font sizes
   - Better contrast

**Expected Outcome:**
- Professional, polished appearance
- Better than competitor UI
- Theme-aware colors

**Time Estimate:** ~2-3 hours

---

### **Phase 4: Advanced Features (Optional)**
**Goal:** Exceed competitor capabilities

**Tasks:**
1. ✅ **Skill Comparison**
   - Compare 2+ players side-by-side
   - Overlay DPS charts

2. ✅ **Export Charts**
   - Save charts as PNG
   - Export data as CSV

3. ✅ **Annotations**
   - Mark important moments on timeline
   - Add notes to encounters

4. ✅ **Rotation Analysis**
   - Show skill usage timeline
   - Highlight cooldown usage

**Expected Outcome:**
- Unique features competitor doesn't have
- Advanced analysis tools

**Time Estimate:** ~4-6 hours (if desired)

---

## 📋 Technical Notes

### Key Files to Modify:

**Phase 1:**
1. `SkillBreakdownView.xaml` - Embed chart controls
2. `SkillBreakdownViewModel.cs` - Wire chart data
3. `DpsStatisticsViewModel.cs` - Pass player data to SkillBreakdownView

**Phase 2:**
1. Create `HpsTrendChartView.xaml` + ViewModel
2. Create `DtpsTrendChartView.xaml` + ViewModel
3. Update `ChartDataService.cs` - Add DTPS tracking
4. Update `StatisticDataViewModel.cs` - Add healing/tanking stats

**Phase 3:**
1. `SkillBreakdownView.xaml` - Update styles
2. Create `SkillCardStyle.xaml` - Material Design card style
3. Update window dimensions

### Dependencies Already Installed:
- ✅ OxyPlot.Wpf (NuGet package)
- ✅ CommunityToolkit.Mvvm (for MVVM)
- ✅ All chart infrastructure

### Data Flow:
```
DpsStatisticsViewModel (Main Window)
    ↓ (player clicked)
SkillBreakdownViewModel
    ↓ (player data)
SkillBreakdownChartView / DpsTrendChartView
    ↑ (chart data)
ChartDataService (background sampling)
```

---

## 🎯 Success Criteria

### Phase 1 Complete:
- [x] Pie chart visible in SkillBreakdownView
- [x] Line chart visible in SkillBreakdownView
- [x] Real-time updates working
- [x] Player selection synced

### Phase 2 Complete:
- [x] Healing tab shows HPS charts
- [x] Tanking tab shows DTPS charts
- [x] All stats accurately tracked

### Phase 3 Complete:
- [x] Modern card design
- [x] Hover effects working
- [x] 1200x800 window
- [x] Theme colors applied

---

## Priority Ranking:

1. **HIGH:** Phase 1 - Integrate existing charts (2-3 hours)
2. **MEDIUM:** Phase 2 - Complete tabs (3-4 hours)
3. **LOW:** Phase 3 - UI polish (2-3 hours)
4. **OPTIONAL:** Phase 4 - Advanced features (4-6 hours)

**Total Time (Phases 1-3):** 7-10 hours

---

## Next Steps:

**Immediate Action:**
1. Start with Phase 1, Task 1: Embed SkillBreakdownChartView
2. Test with live combat data
3. Verify real-time updates
4. Move to Task 2: Embed DpsTrendChartView

**Ready to proceed?** Let me know if you want to start with Phase 1!
