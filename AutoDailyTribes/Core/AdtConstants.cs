namespace AutoDailyTribes.Core;

internal static class AdtConstants
{
    public const int DailyAllowanceCap = 12;
    public const int MaxAcceptsPerTribe = 3;
    public const int MaxTribeRank = 8;

    public const string PrimaryCommand = "/adt";
    public const string AliasCommand = "/dailytribes";

    internal static class ThrottleKeys
    {
        public const string Save = "AutoDailyTribes.Save";
    }

    public const int SaveThrottleMs = 500;
}
