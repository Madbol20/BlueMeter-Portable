using System;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Service to determine which holiday theme should be active based on current date
/// </summary>
public static class HolidayThemeService
{
    /// <summary>
    /// Get the appropriate holiday theme ID for the current date
    /// Returns null if no holiday is active
    /// </summary>
    public static string? GetCurrentHolidayTheme()
    {
        var now = DateTime.Now;

        // Christmas: December 1st - December 30th
        if (now.Month == 12 && now.Day >= 1 && now.Day <= 30)
        {
            return "Christmas";
        }

        // Future holidays can be added here:
        // Halloween: October 25th - October 31st
        // if (now.Month == 10 && now.Day >= 25 && now.Day <= 31)
        // {
        //     return "Halloween";
        // }

        // New Year: December 31st - January 2nd
        // if ((now.Month == 12 && now.Day == 31) || (now.Month == 1 && now.Day <= 2))
        // {
        //     return "NewYear";
        // }

        return null;
    }

    /// <summary>
    /// Check if a holiday theme is currently active
    /// </summary>
    public static bool IsHolidayActive()
    {
        return GetCurrentHolidayTheme() != null;
    }

    /// <summary>
    /// Get the display name for the current holiday
    /// </summary>
    public static string? GetCurrentHolidayName()
    {
        return GetCurrentHolidayTheme() switch
        {
            "Christmas" => "Christmas 🎄",
            "Halloween" => "Halloween 🎃",
            "NewYear" => "New Year 🎆",
            _ => null
        };
    }
}
