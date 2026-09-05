namespace AutoDailyTribes.Windows;

internal static class Formatting
{
    private static readonly DateTime ResetEpoch = new(2000, 1, 1, 15, 0, 0, DateTimeKind.Utc);

    public static string Clock(long milliseconds)
    {
        var total = (int)(milliseconds / 1000);
        return $"{total / 60:D2}:{total % 60:D2}";
    }

    public static string Elapsed(TimeSpan span)
    {
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
        return $"{span.Minutes}m {span.Seconds:D2}s";
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
        if (remaining.TotalHours >= 1) return $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
        if (remaining.TotalMinutes >= 1) return $"{remaining.Minutes}m {remaining.Seconds:D2}s";
        return $"{remaining.Seconds}s";
    }

    public static string LocalResetTime()
    {
        var now = DateTime.UtcNow;
        var reset = ResetEpoch.AddDays(Math.Floor((now - ResetEpoch).TotalDays) + 1);
        return reset.ToLocalTime().ToString("HH:mm");
    }

    public static string Plural(int count, string singular, string plural)
        => count == 1 ? $"{count} {singular}" : $"{count} {plural}";
}
