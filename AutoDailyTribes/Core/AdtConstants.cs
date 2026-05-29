namespace AutoDailyTribes.Core;

internal static class AdtConstants
{
    public const int DailyAllowanceCap = 12;
    public const int MaxAcceptsPerTribe = 3;
    public const int MaxTribeRank = 8;

    // ~60fps. Time Questionable gets to spin up a single quest before we assume it never engaged,
    // and the overall ceiling for one quest so a hung Questionable run can't lock the whole tribe.
    public const int QuestStartFrames = 300;     // ~5s
    public const int QuestRunFrames = 18000;     // ~5 min

    // Frames to wait for an EquipGearset request to actually land on the target job.
    public const int GearsetSwitchFrames = 180;  // ~3s

    public const string PrimaryCommand = "/adt";
    public const string AliasCommand = "/dailytribes";

    internal static class ThrottleKeys
    {
        public const string Save = "AutoDailyTribes.Save";
    }

    public const int SaveThrottleMs = 500;
}
