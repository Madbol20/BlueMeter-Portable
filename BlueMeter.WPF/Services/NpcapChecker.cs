using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace BlueMeter.WPF.Services;

public static class NpcapChecker
{
    /// <summary>
    /// Checks if Npcap or WinPcap is installed on the system
    /// </summary>
    public static bool IsNpcapInstalled()
    {
        // Check for Npcap in registry
        if (RegKeyExists(@"SOFTWARE\Npcap"))
            return true;

        // Check for WinPcap in registry (legacy)
        if (RegKeyExists(@"SOFTWARE\WinPcap"))
            return true;

        // Check for Npcap DLL in system32
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (File.Exists(Path.Combine(system32, "Npcap", "wpcap.dll")))
            return true;

        // Check for WinPcap DLL in system32 (legacy)
        if (File.Exists(Path.Combine(system32, "wpcap.dll")))
            return true;

        return false;
    }

    private static bool RegKeyExists(string keyPath)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Shows a dialog informing the user that Npcap is required
    /// </summary>
    public static void ShowNpcapRequiredDialog()
    {
        var result = MessageBox.Show(
            "BlueMeter requires Npcap to capture network packets.\n\n" +
            "Npcap is not currently installed on your system.\n\n" +
            "Would you like to download Npcap now?\n\n" +
            "(The application will exit after you click OK or Cancel)",
            "Npcap Required",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://npcap.com/#download",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently fail if browser doesn't open
            }
        }
    }
}
