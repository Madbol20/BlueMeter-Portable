using Microsoft.Extensions.Logging;
using BlueMeter.WPF.Data;
using BlueProto;
using BlueMeter.Core.Data;

namespace BlueMeter.Core.Analyze.V2.Processors;

/// <summary>
/// Processes team/matching-related messages to detect queue pops
/// </summary>
internal sealed class TeamMatchingProcessor : IMessageProcessor, IDisposable
{
    private readonly IDataStorage _storage;
    private readonly ILogger? _logger;
    private readonly object _lock = new();

    public TeamMatchingProcessor(IDataStorage storage, ILogger? logger = null)
    {
        _storage = storage;
        _logger = logger;

        // NOTE: Message gap detection disabled - using Return message burst detection instead
        // CharTeam.IsMatching field is always NULL in this game, so cannot be used for detection
    }

    public void Dispose()
    {
        // Timer removed - no longer using gap-based detection
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

            // DEBUG: Log CharTeam fields to identify queue pop patterns
            if (DataStorageV2.EnableQueueDetectionLogging)
            {
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "charteam-messages.log");
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var charIds = string.Join(", ", charTeam.CharIds);
                    var teamMemberCount = charTeam.TeamMemberData?.Count ?? 0;

                    var logMsg = $"[{timestamp}] CharTeam - " +
                        $"TeamId: {(charTeam.HasTeamId ? charTeam.TeamId.ToString() : "NULL")}, " +
                        $"LeaderId: {(charTeam.HasLeaderId ? charTeam.LeaderId.ToString() : "NULL")}, " +
                        $"TeamNum: {(charTeam.HasTeamNum ? charTeam.TeamNum.ToString() : "NULL")}, " +
                        $"IsMatching: {(charTeam.HasIsMatching ? charTeam.IsMatching.ToString() : "NULL")}, " +
                        $"MemberCount: {teamMemberCount}, " +
                        $"CharIds: [{charIds}]\n";

                    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                    File.AppendAllText(logPath, logMsg);
                }
                catch { }
            }

            // Note: Queue pop detection is now handled by Return message burst detection
            // CharTeam.IsMatching field is always NULL and cannot be used for detection
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[TeamMatchingProcessor] Error processing CharTeam message");
        }
    }
}
