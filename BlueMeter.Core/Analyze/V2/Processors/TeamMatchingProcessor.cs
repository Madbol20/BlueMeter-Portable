using Microsoft.Extensions.Logging;
using BlueMeter.WPF.Data;
using BlueProto;

namespace BlueMeter.Core.Analyze.V2.Processors;

/// <summary>
/// Processes team/matching-related messages to detect queue pops
/// </summary>
internal sealed class TeamMatchingProcessor : IMessageProcessor
{
    private readonly IDataStorage _storage;
    private readonly ILogger? _logger;
    private bool _wasMatching = false; // Track previous matching state

    public TeamMatchingProcessor(IDataStorage storage, ILogger? logger = null)
    {
        _storage = storage;
        _logger = logger;
    }

    public void Process(byte[] payload)
    {
        try
        {
            // Try to parse CharTeam message
            var charTeam = CharTeam.Parser.ParseFrom(payload);

            if (charTeam == null)
            {
                _logger?.LogWarning("[TeamMatchingProcessor] Failed to parse CharTeam message");
                return;
            }

            // Log team info if queue detection logging is enabled
            if (Data.DataStorageV2.EnableQueueDetectionLogging)
            {
                _logger?.LogInformation(
                    "[TeamMatchingProcessor] CharTeam parsed - TeamId: {TeamId}, LeaderId: {LeaderId}, IsMatching: {IsMatching}, TeamNum: {TeamNum}",
                    charTeam.HasTeamId ? charTeam.TeamId : "N/A",
                    charTeam.HasLeaderId ? charTeam.LeaderId : "N/A",
                    charTeam.HasIsMatching ? charTeam.IsMatching : false,
                    charTeam.HasTeamNum ? charTeam.TeamNum : 0);
            }

            // Detect queue pop: IsMatching changed from true to false
            if (charTeam.HasIsMatching)
            {
                var currentMatching = charTeam.IsMatching;

                if (_wasMatching && !currentMatching)
                {
                    // Queue pop detected! Matching state changed from true to false
                    _logger?.LogWarning("[TeamMatchingProcessor] 🎉 QUEUE POP DETECTED! IsMatching changed from true to false");

                    // Fire the queue pop event if DataStorage supports it
                    if (_storage is Data.DataStorageV2 dataStorageV2)
                    {
                        dataStorageV2.TriggerQueuePopDetected();
                    }
                }

                _wasMatching = currentMatching;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[TeamMatchingProcessor] Error processing CharTeam message");
        }
    }
}
