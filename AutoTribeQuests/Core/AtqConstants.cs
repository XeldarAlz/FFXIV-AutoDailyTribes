namespace AutoTribeQuests.Core;

// Prefixed AtqConstants (not Constants) to avoid colliding with ECommons.Constants.
internal static class AtqConstants
{
    public const int DailyAllowanceCap = 12;
    public const int MaxAcceptsPerTribe = 3;

    public const string PrimaryCommand = "/atq";
    public const string AliasCommand = "/tribequests";

    internal static class ThrottleKeys
    {
        public const string Save = "AutoTribeQuests.Save";
    }

    public const int SaveThrottleMs = 500;
}
