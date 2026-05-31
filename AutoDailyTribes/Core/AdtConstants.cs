namespace AutoDailyTribes.Core;

internal static class AdtConstants
{
    public const int DailyAllowanceCap = 12;
    public const int MaxAcceptsPerTribe = 3;
    public const int MaxTribeRank = 8;

    // Questionable delegation, wall-clock so high frame rates can't shrink it. A quest is "done" when it
    // leaves the journal (Questionable does its objectives AND turn-in), not when IsRunning dips — that
    // flag goes false between steps. QuestCompleteTimeoutMs is the overall ceiling for one quest;
    // QuestIdleRestartMs is how long Questionable may sit idle mid-quest before we re-issue StartSingleQuest;
    // MaxQuestRestarts caps those re-issues so a quest Questionable genuinely can't progress is skipped.
    public const int QuestCompleteTimeoutMs = 600_000; // 10 min
    public const int QuestIdleRestartMs = 3_000;
    public const int MaxQuestRestarts = 5;

    // Job switching. Wall-clock so it doesn't shrink at high frame rates: time to wait for a clear
    // (non-event/non-combat/non-zoning) window before dispatching, the overall ceiling for the swap to
    // land on the target job, and how often to re-dispatch EquipGearset while waiting (a single request
    // can be silently dropped during a transient lock right after the previous tribe's NPC dialog).
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
