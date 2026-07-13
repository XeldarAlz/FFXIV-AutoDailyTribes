namespace AutoDailyTribes.Core;

internal static class AdtConstants
{
    public const int DailyAllowanceCap = 12;
    public const int MaxAcceptsPerTribe = 3;
    public const int MaxTribeRank = 8;

    public const int QuestCompleteTimeoutMs = 600_000; // 10 min
    public const int QuestIdleRestartMs = 3_000;
    public const int MaxQuestRestarts = 5;

    public const int QuestStuckMs = 120_000; // 2 min

    public const int JobSwitchReadyMs = 5_000;
    public const int JobSwitchConfirmMs = 8_000;
    public const int JobSwitchRedispatchMs = 500;

    public const string PrimaryCommand = "/adt";
    public const string AliasCommand = "/dailytribes";

    // Some plugin commands misbehave when several arrive in the same frame, so queued
    // post-run commands are spaced out rather than fired back-to-back.
    public const int PostRunCommandSpacingMs = 500;

    internal static class ThrottleKeys
    {
        public const string Save = "AutoDailyTribes.Save";
    }

    public const int SaveThrottleMs = 500;
}
