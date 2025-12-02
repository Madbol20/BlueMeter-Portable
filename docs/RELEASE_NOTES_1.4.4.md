# BlueMeter v1.4.4 Release Notes

## 🐛 Bug Fixes

### Critical Crash Fix: Mouse-Through Mode Interaction

**Issue**: Application crashed when interacting with the damage meter window after disabling mouse-through mode following a long raid.

**Error**: `System.Windows.Markup.XamlParseException: A TwoWay or OneWayToSource binding cannot work on the read-only property 'EffectiveDamage'`

**Problems Identified**:
1. **XAML Binding Mode**: WPF attempted to create two-way bindings on read-only computed properties
2. **Template Context**: Bindings within popup templates defaulted to TwoWay mode in certain contexts
3. **Crash Trigger**: Occurred when hovering over player stats after untoggling mouse-through mode

**Fixes Applied**:

#### 1. Fixed EffectiveDamage Binding (`DpsStatisticsView.xaml:308`)
- **Explicit OneWay mode** added to `EffectiveDamage` binding
- Prevents WPF from attempting two-way binding on read-only property
- Property computes: `DamageTaken + DamageMitigated`

#### 2. Fixed MitigationPercent Binding (`DpsStatisticsView.xaml:321`)
- **Explicit OneWay mode** added to `MitigationPercent` binding
- Property computes: `(DamageMitigated / EffectiveDamage) × 100`

#### 3. Fixed EffectiveTps Binding (`DpsStatisticsView.xaml:327`)
- **Explicit OneWay mode** added to `EffectiveTps` binding
- Property computes: `EffectiveDamage / Duration`

---

## 🔧 Technical Details

### Root Cause Analysis

**The Problem**:
- Three computed properties in `StatisticDataViewModel` are read-only (no setters):
  - `EffectiveDamage` (line 26)
  - `MitigationPercent` (line 31)
  - `EffectiveTps` (line 38)
- XAML bindings in the `PopupTemplate` didn't explicitly specify `Mode=OneWay`
- WPF's default binding mode in template contexts sometimes defaults to `TwoWay`
- When popup was instantiated during mouse-over, WPF tried to create two-way binding
- **Result**: Immediate crash with XamlParseException

**The Solution**:
- Add explicit `Mode=OneWay` to all three bindings
- Forces WPF to use one-way binding regardless of context
- Properties remain read-only as designed
- Popup template loads successfully

### When Did This Crash Occur?

The crash was triggered by this specific sequence:
1. Long raid in progress (damage meter tracking data)
2. Mouse-through mode enabled during raid
3. User disables mouse-through mode
4. User hovers mouse over player stats to view popup
5. WPF attempts to render popup template
6. **CRASH**: Two-way binding fails on read-only property

---

## 🎯 What's Fixed

- ✅ No more crashes when interacting with damage meter after disabling mouse-through mode
- ✅ Tank stats popup displays correctly (Effective Damage, Mitigation %, Effective TPS)
- ✅ All popup tooltips work reliably
- ✅ Computed properties remain read-only (correct design)

---

## 📦 Installation

Download the latest release and extract to your preferred location. This hotfix resolves a critical crash affecting users who toggle mouse-through mode during raids.

---

**Thank you for using BlueMeter!** 🎉
