using System.Collections.ObjectModel;
using BlueMeter.WPF.Models;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Service for collecting and managing chart data in real-time
/// Samples DPS/HPS data at regular intervals for chart visualization
/// </summary>
public interface IChartDataService : IDisposable
{
    /// <summary>
    /// Start background sampling timer
    /// </summary>
    void Start();

    /// <summary>
    /// Stop background sampling timer
    /// </summary>
    void Stop();

    /// <summary>
    /// Get DPS history for a specific player
    /// </summary>
    /// <param name="playerId">Player UID</param>
    /// <returns>Observable collection of DPS data points, or null if player not found</returns>
    ObservableCollection<ChartDataPoint>? GetDpsHistory(long playerId);

    /// <summary>
    /// Get HPS history for a specific player
    /// </summary>
    /// <param name="playerId">Player UID</param>
    /// <returns>Observable collection of HPS data points, or null if player not found</returns>
    ObservableCollection<ChartDataPoint>? GetHpsHistory(long playerId);

    /// <summary>
    /// Get all player IDs that have chart data
    /// </summary>
    /// <returns>Collection of player UIDs</returns>
    IReadOnlyCollection<long> GetTrackedPlayerIds();

    /// <summary>
    /// Indicates if the service is currently sampling data
    /// </summary>
    bool IsRunning { get; }
}
