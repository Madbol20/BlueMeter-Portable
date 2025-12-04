using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlueMeter.Core.Analyze;
using BlueMeter.Core.Analyze.Models;
using BlueMeter.Core.Data.Models;

namespace BlueMeter.Core.Data;

/// <summary>
/// Manages BSON combat log files with automatic rolling window cleanup
/// Keeps only the most recent N encounters, automatically deleting oldest
/// </summary>
public class BattleLogManager
{
    private readonly string _logDirectory;
    private readonly int _maxEncounters;

    /// <summary>
    /// Directory where battle logs are stored
    /// </summary>
    public string LogDirectory => _logDirectory;

    /// <summary>
    /// Maximum number of encounters to store
    /// </summary>
    public int MaxEncounters => _maxEncounters;

    public BattleLogManager(
        string? logDirectory = null,
        int maxEncounters = 10)
    {
        _maxEncounters = maxEncounters > 0 ? maxEncounters : 10;
        _logDirectory = logDirectory ?? GetDefaultLogDirectory();

        // Ensure directory exists
        Directory.CreateDirectory(_logDirectory);

        Console.WriteLine($"[BattleLogManager] Initialized:");
        Console.WriteLine($"  Directory: {_logDirectory}");
        Console.WriteLine($"  Max encounters: {_maxEncounters}");
    }

    /// <summary>
    /// Get default log directory
    /// </summary>
    private static string GetDefaultLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "BlueMeter", "CombatLogs");
    }

    /// <summary>
    /// Save a new encounter and cleanup old ones if needed
    /// </summary>
    public async Task SaveEncounterAsync(
        string encounterId,
        string? bossName,
        List<BattleLog> events,
        List<PlayerInfoFileData> playerInfos)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("[BattleLogManager] No events to save, skipping");
            return;
        }

        try
        {
            // 1. Generate filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sanitizedBossName = SanitizeFileName(bossName ?? "Unknown");
            var sanitizedEncounterId = SanitizeFileName(encounterId.Length > 8 ? encounterId.Substring(0, 8) : encounterId);
            var fileName = $"{timestamp}_{sanitizedEncounterId}_{sanitizedBossName}.bmlogs";
            var filePath = Path.Combine(_logDirectory, fileName);

            Console.WriteLine($"[BattleLogManager] Saving encounter:");
            Console.WriteLine($"  EncounterId: {encounterId}");
            Console.WriteLine($"  Boss: {bossName}");
            Console.WriteLine($"  Events: {events.Count}");
            Console.WriteLine($"  File: {fileName}");

            // 2. Convert BattleLog to BattleLogFileData (using implicit operator)
            var battleLogFileData = events.Select(log => (BattleLogFileData)log).ToArray();

            // 3. Create logs file structure
            var logsFile = new LogsFileV3_0_0
            {
                FileVersion = LogsFileVersion.V3_0_0,
                PlayerInfos = playerInfos.ToArray(),
                BattleLogs = battleLogFileData
            };

            // 4. Save to BSON file
            await BattleLogWriter.WriteToFileAsync(_logDirectory, logsFile);

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                Console.WriteLine($"[BattleLogManager] Encounter saved: {FormatBytes(fileInfo.Length)}");
            }
            else
            {
                Console.WriteLine($"[BattleLogManager] WARNING: File was not created!");
            }

            // 5. Cleanup old files
            await CleanupOldEncountersAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BattleLogManager] ERROR saving encounter: {ex.Message}");
            Console.WriteLine($"  Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Remove oldest encounter files if limit exceeded
    /// </summary>
    private async Task CleanupOldEncountersAsync()
    {
        try
        {
            // Get all .bmlogs files sorted by creation time (oldest first)
            var files = Directory.GetFiles(_logDirectory, "*.bmlogs")
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.CreationTimeUtc)  // Oldest first
                .ToList();

            var currentCount = files.Count;

            if (currentCount <= _maxEncounters)
            {
                Console.WriteLine($"[BattleLogManager] No cleanup needed: {currentCount}/{_maxEncounters} encounters");
                return;
            }

            // Calculate how many to delete
            var toDelete = currentCount - _maxEncounters;
            Console.WriteLine($"[BattleLogManager] Cleaning up {toDelete} old encounters ({currentCount}/{_maxEncounters})");

            // Delete oldest files
            for (int i = 0; i < toDelete; i++)
            {
                var file = files[i];
                Console.WriteLine($"[BattleLogManager] Deleting old encounter: {file.Name} ({FormatBytes(file.Length)})");

                await Task.Run(() => file.Delete());
            }

            Console.WriteLine($"[BattleLogManager] Cleanup completed: {currentCount - toDelete} encounters remaining");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BattleLogManager] ERROR during cleanup: {ex.Message}");
            // Don't throw - cleanup failure shouldn't break saving
        }
    }

    /// <summary>
    /// Get list of all stored encounters
    /// </summary>
    public List<EncounterFileInfo> GetStoredEncounters()
    {
        try
        {
            var files = Directory.GetFiles(_logDirectory, "*.bmlogs")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)  // Newest first for display
                .Select(f => ParseFileName(f))
                .Where(info => info != null)
                .Cast<EncounterFileInfo>()
                .ToList();

            Console.WriteLine($"[BattleLogManager] Found {files.Count} stored encounters");
            return files;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BattleLogManager] ERROR getting stored encounters: {ex.Message}");
            return new List<EncounterFileInfo>();
        }
    }

    /// <summary>
    /// Get total disk usage in bytes
    /// </summary>
    public long GetTotalDiskUsageBytes()
    {
        try
        {
            return Directory.GetFiles(_logDirectory, "*.bmlogs")
                .Select(f => new FileInfo(f).Length)
                .Sum();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BattleLogManager] ERROR calculating disk usage: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Delete specific encounter
    /// </summary>
    public async Task DeleteEncounterAsync(string fileName)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Encounter file not found: {fileName}");

        Console.WriteLine($"[BattleLogManager] Deleting encounter {fileName}");
        await Task.Run(() => File.Delete(filePath));
    }

    /// <summary>
    /// Delete all encounters
    /// </summary>
    public async Task DeleteAllEncountersAsync()
    {
        try
        {
            var files = Directory.GetFiles(_logDirectory, "*.bmlogs");
            Console.WriteLine($"[BattleLogManager] Deleting {files.Length} encounters");

            foreach (var file in files)
            {
                await Task.Run(() => File.Delete(file));
            }

            Console.WriteLine($"[BattleLogManager] All encounters deleted");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BattleLogManager] ERROR deleting encounters: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parse filename into structured info
    /// </summary>
    private EncounterFileInfo? ParseFileName(FileInfo file)
    {
        try
        {
            // Expected format: 20251203_103000_abc12345_Boss-Name.bmlogs
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
            var parts = nameWithoutExt.Split('_', 4);

            if (parts.Length < 3)
            {
                Console.WriteLine($"[BattleLogManager] Invalid filename format: {file.Name}");
                return null;
            }

            var dateStr = parts[0];
            var timeStr = parts[1];
            var encounterId = parts[2];
            var bossName = parts.Length > 3 ? parts[3] : "Unknown";

            // Parse timestamp
            var timestampStr = $"{dateStr}_{timeStr}";
            if (!DateTime.TryParseExact(timestampStr, "yyyyMMdd_HHmmss", null,
                System.Globalization.DateTimeStyles.None, out var timestamp))
            {
                timestamp = file.CreationTimeUtc;
            }

            return new EncounterFileInfo
            {
                FileName = file.Name,
                EncounterId = encounterId,
                BossName = bossName,
                Timestamp = timestamp,
                SizeBytes = file.Length,
                CreatedUtc = file.CreationTimeUtc
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BattleLogManager] Error parsing filename {file.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sanitize filename by removing invalid characters
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Unknown";

        // Remove invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", name.Split(invalid));

        // Replace spaces with hyphens
        sanitized = sanitized.Replace(' ', '-');

        // Limit length
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);

        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }

    /// <summary>
    /// Format bytes to human-readable string
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Information about a stored encounter file
/// </summary>
public class EncounterFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public string EncounterId { get; init; } = string.Empty;
    public string BossName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public long SizeBytes { get; init; }
    public DateTime CreatedUtc { get; init; }

    public string FormattedSize => FormatBytes(SizeBytes);
    public string FormattedDate => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
