using System.Reflection;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace BlueMeter.WPF.Services;

public sealed class UpdateCheckService : IUpdateCheckService
{
    private readonly ILogger<UpdateCheckService> _logger;
    private static readonly HttpClient HttpClient = new();

    public UpdateCheckService(ILogger<UpdateCheckService> logger)
    {
        _logger = logger;
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "BlueMeter-UpdateChecker");
    }

    public async Task<string?> CheckForUpdateAsync()
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            var latestVersion = await GetLatestReleaseVersionAsync();

            if (latestVersion == null)
            {
                return null;
            }

            // Compare versions
            if (IsNewerVersion(latestVersion, currentVersion))
            {
                _logger.LogInformation("New version available: {NewVersion} (current: {CurrentVersion})", latestVersion, currentVersion);
                return latestVersion;
            }

            _logger.LogDebug("No new version available. Current: {CurrentVersion}, Latest: {LatestVersion}", currentVersion, latestVersion);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates");
            return null;
        }
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    private async Task<string?> GetLatestReleaseVersionAsync()
    {
        try
        {
            // Fetch latest release from GitHub API
            var response = await HttpClient.GetAsync("https://api.github.com/repos/caaatto/BlueMeter/releases/latest");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("tag_name", out var tagElement))
            {
                var tagName = tagElement.GetString();
                // Remove 'v' prefix if present
                return tagName?.TrimStart('v');
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch latest release from GitHub");
            return null;
        }
    }

    private static bool IsNewerVersion(string? newVersion, string? currentVersion)
    {
        if (string.IsNullOrEmpty(newVersion) || string.IsNullOrEmpty(currentVersion))
        {
            return false;
        }

        try
        {
            var parts1 = newVersion.Split('.').Select(int.Parse).ToArray();
            var parts2 = currentVersion.Split('.').Select(int.Parse).ToArray();

            // Pad with zeros if lengths differ
            var maxLen = Math.Max(parts1.Length, parts2.Length);
            Array.Resize(ref parts1, maxLen);
            Array.Resize(ref parts2, maxLen);

            for (int i = 0; i < maxLen; i++)
            {
                if (parts1[i] > parts2[i]) return true;
                if (parts1[i] < parts2[i]) return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
