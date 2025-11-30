namespace BlueMeter.Core.Analyze;

public enum MessageMethod : uint
{
    SyncNearEntities = 0x00000006U,
    SyncContainerData = 0x00000015U,
    SyncContainerDirtyData = 0x00000016U,
    SyncToMeDeltaInfo = 0x0000002EU,
    SyncNearDeltaInfo = 0x0000002DU,

    // CharTeam messages - detected through logging during queue pops
    // This ID appears regularly (~5s intervals) and may track team/matching status
    TeamMatching = 0x0000002BU, // Method ID 43 (decimal)
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