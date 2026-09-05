using AutoDailyTribes.Core.Localization;

namespace AutoDailyTribes.Windows;

internal static class Formatting
{
    private static readonly DateTime ResetEpoch = new(2000, 1, 1, 15, 0, 0, DateTimeKind.Utc);

    public static string Clock(long milliseconds)
    {
        var total = (int)(milliseconds / 1000);
        return string.Format(Loc.Culture, "{0:D2}:{1:D2}", total / 60, total % 60);
    }

    public static string Elapsed(TimeSpan span)
    {
        if (span.TotalHours >= 1) return string.Format(Loc.Culture, "{0}h {1:D2}m", (int)span.TotalHours, span.Minutes);
        return string.Format(Loc.Culture, "{0}m {1:D2}s", span.Minutes, span.Seconds);
    }

    public static TimeSpan UntilDailyReset()
    {
        var now = DateTime.UtcNow;
        var sinceEpoch = now - ResetEpoch;
        var nextReset = ResetEpoch.AddDays(Math.Floor(sinceEpoch.TotalDays) + 1);
        return nextReset - now;
    }

    public static string ResetCountdown()
    {
        var remaining = UntilDailyReset();
        if (remaining.TotalHours >= 1) return string.Format(Loc.Culture, "{0}h {1:D2}m", (int)remaining.TotalHours, remaining.Minutes);
        if (remaining.TotalMinutes >= 1) return string.Format(Loc.Culture, "{0}m {1:D2}s", remaining.Minutes, remaining.Seconds);
        return string.Format(Loc.Culture, "{0}s", remaining.Seconds);
    }

    public static string LocalResetTime()
    {
        var now = DateTime.UtcNow;
        var reset = ResetEpoch.AddDays(Math.Floor((now - ResetEpoch).TotalDays) + 1);
        return reset.ToLocalTime().ToString("HH:mm", Loc.Culture);
    }

    public static string Tribes(int count) => Loc.Plural(L.Common.TribesCount, count);

    public static string Number(int value) => value.ToString(Loc.Culture);
}
