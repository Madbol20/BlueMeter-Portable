namespace BlueMeter.WPF.Services;

public interface IUpdateCheckService
{
    /// <summary>
    /// Checks if a new version is available on GitHub
    /// </summary>
    /// <returns>Version string if update available, null otherwise</returns>
    Task<string?> CheckForUpdateAsync();

    /// <summary>
    /// Gets the current application version
    /// </summary>
    string GetCurrentVersion();
}
