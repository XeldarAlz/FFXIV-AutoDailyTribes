namespace AutoTribeQuests.Core;

internal static class Constants
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
