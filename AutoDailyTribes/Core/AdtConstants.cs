namespace AutoDailyTribes.Core;

internal static class AdtConstants
{
    public const int DailyAllowanceCap = 12;
    public const int MaxAcceptsPerTribe = 3;
    public const int MaxTribeRank = 8;

    public const int QuestCompleteTimeoutMs = 600_000; // 10 min
    public const int QuestIdleRestartMs = 3_000;
    public const int MaxQuestRestarts = 5;

    // Questionable reports IsRunning()==true even while wedged inside a step (e.g. a fishing
    // quest it can't finish), so a single quest that shows no turn-in and no quest change for
    // this long is treated as stuck and dropped from the batch.
    public const int QuestStuckMs = 120_000; // 2 min

    public const int JobSwitchReadyMs = 5_000;
    public const int JobSwitchConfirmMs = 8_000;
    public const int JobSwitchRedispatchMs = 500;

    public const string PrimaryCommand = "/adt";
    public const string AliasCommand = "/dailytribes";

    internal static class ThrottleKeys
    {
        public const string Save = "AutoDailyTribes.Save";
    }

    public const int SaveThrottleMs = 500;
}
