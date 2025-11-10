using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Translates skill names from Chinese to English using a static dictionary
/// </summary>
public class DeepLSkillTranslator
{
    private readonly ILogger<DeepLSkillTranslator>? _logger;
    private readonly Dictionary<string, string> _translationCache;

    public DeepLSkillTranslator(string apiKeyOrPath = "", ILogger<DeepLSkillTranslator>? logger = null)
    {
        _logger = logger;
        _translationCache = new Dictionary<string, string>();

        // Load static translations from JSON file
        LoadStaticTranslations();
    }

    /// <summary>
    /// Load static translations from skills_en.json file
    /// </summary>
    private void LoadStaticTranslations()
    {
        try
        {
            var translationFile = Path.Combine(AppContext.BaseDirectory, "Data", "skills_en.json");

            Console.WriteLine($"[DeepLSkillTranslator] Attempting to load from: {translationFile}");

            if (File.Exists(translationFile))
            {
                var json = File.ReadAllText(translationFile);
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (translations != null)
                {
                    foreach (var kvp in translations)
                    {
                        _translationCache[kvp.Key] = kvp.Value;
                    }
                    var count = _translationCache.Count;
                    Console.WriteLine($"[DeepLSkillTranslator] Successfully loaded {count} skill translations");
                    _logger?.LogInformation($"Loaded {count} skill translations from file");
                }
            }
            else
            {
                Console.WriteLine($"[DeepLSkillTranslator] File not found at: {translationFile}");
                _logger?.LogWarning($"Translation file not found: {translationFile}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeepLSkillTranslator] ERROR loading translations: {ex.Message}\n{ex.StackTrace}");
            _logger?.LogWarning($"Failed to load static translations: {ex.Message}");
        }
    }

    /// <summary>
    /// Translate Chinese skill name to English using static dictionary
    /// </summary>
    public Task<string> TranslateAsync(string chineseText)
    {
        if (string.IsNullOrEmpty(chineseText))
            return Task.FromResult(chineseText);

        if (_translationCache.TryGetValue(chineseText, out var translation))
        {
            return Task.FromResult(translation);
        }

        // Return original if not found in translations
        return Task.FromResult(chineseText);
    }

    /// <summary>
    /// Synchronous wrapper for translation
    /// Uses static dictionary, instant lookup
    /// </summary>
    public string Translate(string chineseText)
    {
        if (string.IsNullOrEmpty(chineseText))
            return chineseText;

        if (_translationCache.TryGetValue(chineseText, out var translation))
        {
            _logger?.LogDebug($"Found translation: '{chineseText}' -> '{translation}'");
            return translation;
        }

        // Return original if not found
        return chineseText;
    }

    /// <summary>
    /// Get all loaded translations
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAllTranslations()
    {
        return _translationCache.AsReadOnly();
    }

    /// <summary>
    /// Clear the translation cache
    /// </summary>
    public void ClearCache()
    {
        _translationCache.Clear();
        _logger?.LogInformation("Cleared translation cache");
    }
}
