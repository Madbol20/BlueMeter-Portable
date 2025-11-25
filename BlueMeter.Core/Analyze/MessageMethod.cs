namespace BlueMeter.Core.Analyze;

public enum MessageMethod : uint
{
    SyncNearEntities = 0x00000006U,
    SyncContainerData = 0x00000015U,
    SyncContainerDirtyData = 0x00000016U,
    SyncToMeDeltaInfo = 0x0000002EU,
    SyncNearDeltaInfo = 0x0000002DU,

    // TODO: Find the actual method ID for CharTeam messages
    // To find it: Enable queue detection logging in settings, join a queue,
    // and check the logs for "UNKNOWN METHOD ID" messages
    TeamMatching = 0x00000000U, // PLACEHOLDER - needs to be replaced with actual ID
}

public static class MessageMethodExtensions
{
    public static bool TryParseMessageMethod(string methodName, out MessageMethod method)
    {
        return Enum.TryParse(methodName, ignoreCase: true, out method);
    }

    public static uint ToUInt32(this MessageMethod method)
    {
        return (uint)method;
    }

    public static MessageMethod FromUInt32(uint value)
    {
        return (MessageMethod)value;
    }
}