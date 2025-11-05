using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlueMeter.Core.Data.Database;

/// <summary>
/// Initializes and migrates the BlueMeter SQLite database
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Initialize database and run migrations
    /// </summary>
    public static async Task InitializeAsync(string databasePath)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = CreateOptions(databasePath);

        using var context = new BlueMeterDbContext(options);

        // Create database if it doesn't exist and apply migrations
        await context.Database.EnsureCreatedAsync();

        // Deactivate any active encounters from previous session
        var repository = new EncounterRepository(context);
        await repository.DeactivateAllEncountersAsync();
    }

    /// <summary>
    /// Create DbContext options for the given database path
    /// </summary>
    public static DbContextOptions<BlueMeterDbContext> CreateOptions(string databasePath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BlueMeterDbContext>();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        // Enable sensitive data logging in debug builds
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif

        return optionsBuilder.Options;
    }

    /// <summary>
    /// Create a DbContext factory function
    /// </summary>
    public static Func<BlueMeterDbContext> CreateContextFactory(string databasePath)
    {
        var options = CreateOptions(databasePath);
        return () => new BlueMeterDbContext(options);
    }

    /// <summary>
    /// Get default database path
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var blueMeterPath = Path.Combine(appDataPath, "BlueMeter");

        if (!Directory.Exists(blueMeterPath))
        {
            Directory.CreateDirectory(blueMeterPath);
        }

        return Path.Combine(blueMeterPath, "BlueMeter.db");
    }

    /// <summary>
    /// Backup database to specified path
    /// </summary>
    public static async Task BackupDatabaseAsync(string sourcePath, string backupPath)
    {
        if (!File.Exists(sourcePath))
            return;

        var directory = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await Task.Run(() => File.Copy(sourcePath, backupPath, true));
    }

    /// <summary>
    /// Get database file size in MB
    /// </summary>
    public static double GetDatabaseSizeMB(string databasePath)
    {
        if (!File.Exists(databasePath))
            return 0;

        var fileInfo = new FileInfo(databasePath);
        return fileInfo.Length / (1024.0 * 1024.0);
    }
}
