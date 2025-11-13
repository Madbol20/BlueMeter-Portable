using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Extracts skill names from the game (when playing in English mode) and logs them for database building.
/// This service captures English skill names directly from network packets and stores them by SkillID.
/// </summary>
public class SkillNameExtractor
{
    private readonly ILogger<SkillNameExtractor>? _logger;
    private readonly ConcurrentDictionary<long, string> _skillIdToEnglishName;
    private readonly object _fileLock = new();
    private string _extractedSkillsFile = "";

    public SkillNameExtractor(ILogger<SkillNameExtractor>? logger = null)
    {
        _logger = logger;
        _skillIdToEnglishName = new ConcurrentDictionary<long, string>();
        InitializeExtractionFile();
    }

    /// <summary>
    /// Initialize the extraction file path
    /// </summary>
    private void InitializeExtractionFile()
    {
        try
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir);

            _extractedSkillsFile = Path.Combine(dataDir, "skills_extracted.json");

            // Load existing extracted skills if file exists
            if (File.Exists(_extractedSkillsFile))
            {
                LoadExistingExtractedSkills();
            }

            _logger?.LogInformation($"Skill extraction file initialized at: {_extractedSkillsFile}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize skill extraction file");
        }
    }

    /// <summary>
    /// Load previously extracted skills from file
    /// </summary>
    private void LoadExistingExtractedSkills()
    {
        try
        {
            if (!File.Exists(_extractedSkillsFile)) return;

            var json = File.ReadAllText(_extractedSkillsFile);
            var skillsList = JsonConvert.DeserializeObject<List<SkillExtraction>>(json);

            if (skillsList != null)
            {
                foreach (var skill in skillsList)
                {
                    _skillIdToEnglishName[skill.SkillId] = skill.EnglishName;
                }
                _logger?.LogInformation($"Loaded {_skillIdToEnglishName.Count} previously extracted skills");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load existing extracted skills");
        }
    }

    /// <summary>
    /// Register a new English skill name found in the game
    /// Call this when a skill is used in English mode
    /// </summary>
    public void RegisterSkillName(long skillId, string englishName)
    {
        if (skillId <= 0 || string.IsNullOrWhiteSpace(englishName))
            return;

        try
        {
            bool isNew = _skillIdToEnglishName.TryAdd(skillId, englishName);

            if (isNew)
            {
                _logger?.LogInformation($"New skill registered: {skillId} = {englishName}");
                // Auto-save after each new skill for safety
                SaveExtractedSkills();
            }
            else if (_skillIdToEnglishName[skillId] != englishName)
            {
                // Update if name changed
                _skillIdToEnglishName[skillId] = englishName;
                _logger?.LogInformation($"Skill updated: {skillId} = {englishName}");
                SaveExtractedSkills();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to register skill {skillId}");
        }
    }

    /// <summary>
    /// Save extracted skills to JSON file
    /// </summary>
    public void SaveExtractedSkills()
    {
        try
        {
            lock (_fileLock)
            {
                var skillsList = _skillIdToEnglishName
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => new SkillExtraction
                    {
                        SkillId = kvp.Key,
                        EnglishName = kvp.Value,
                        Timestamp = DateTime.Now
                    })
                    .ToList();

                var json = JsonConvert.SerializeObject(skillsList, Formatting.Indented);
                File.WriteAllText(_extractedSkillsFile, json);

                _logger?.LogInformation($"Saved {skillsList.Count} extracted skills to {_extractedSkillsFile}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save extracted skills");
        }
    }

    /// <summary>
    /// Export extracted skills in the format needed for skills_en.json
    /// Maps Chinese skill names from EmbeddedSkillConfig to English names
    /// </summary>
    public Dictionary<string, string> ExportForSkillsEnJson()
    {
        var result = new Dictionary<string, string>();

        // This would need to be implemented with EmbeddedSkillConfig
        // to map Chinese names to the English names we've collected

        _logger?.LogInformation($"Exported {result.Count} skills for skills_en.json format");
        return result;
    }

    /// <summary>
    /// Get all extracted skills
    /// </summary>
    public IReadOnlyDictionary<long, string> GetAllExtractedSkills()
    {
        return _skillIdToEnglishName.AsReadOnly();
    }

    /// <summary>
    /// Get extraction count
    /// </summary>
    public int GetExtractedSkillCount() => _skillIdToEnglishName.Count;

    /// <summary>
    /// Model for storing extracted skill data
    /// </summary>
    private class SkillExtraction
    {
        [JsonProperty("skillId")]
        public long SkillId { get; set; }

        [JsonProperty("englishName")]
        public string EnglishName { get; set; } = "";

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
