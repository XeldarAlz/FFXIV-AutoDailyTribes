using ECommons.DalamudServices;
using System.Globalization;
using System.Text;

namespace AutoDailyTribes.Core;

public enum RunLogLevel { Info, Warning, Error }

public readonly record struct RunLogLine(DateTime AtUtc, RunLogLevel Level, string Message);

// In-window copy of everything the automation logs, so a bug report can be pasted straight from
// the plugin instead of digging the [ADT] lines out of dalamud.log. Written and read on the
// framework thread only.
public static class RunLog
{
    public const int Capacity = 600;

    private const string TimeFormat = "HH:mm:ss";

    private static readonly RunLogLine[] lines = new RunLogLine[Capacity];
    private static int start;
    private static int count;
    private static int version;

    public static int Count => count;

    public static int Version => version;

    public static RunLogLine At(int index) => lines[(start + index) % Capacity];

    public static void Info(string message)
    {
        Svc.Log.Info($"{AdtConstants.LogPrefix} {message}");
        Push(RunLogLevel.Info, message);
    }

    public static void Warning(string message)
    {
        Svc.Log.Warning($"{AdtConstants.LogPrefix} {message}");
        Push(RunLogLevel.Warning, message);
    }

    public static void Error(string message)
    {
        Svc.Log.Error($"{AdtConstants.LogPrefix} {message}");
        Push(RunLogLevel.Error, message);
    }

    public static void Record(RunLogLevel level, string message) => Push(level, message);

    public static void Clear()
    {
        start = 0;
        count = 0;
        version++;
    }

    public static string ToText()
    {
        var builder = new StringBuilder(count * 96);
        for (var index = 0; index < count; index++)
        {
            var line = At(index);
            builder.Append(Time(line))
                   .Append(' ')
                   .Append(Tag(line.Level))
                   .Append(' ')
                   .AppendLine(line.Message);
        }
        return builder.ToString();
    }

    public static string Time(RunLogLine line) => line.AtUtc.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);

    private static string Tag(RunLogLevel level) => level switch
    {
        RunLogLevel.Warning => "[WARN]",
        RunLogLevel.Error   => "[ERR ]",
        _                   => "[INFO]",
    };

    private static void Push(RunLogLevel level, string message)
    {
        var slot = (start + count) % Capacity;
        lines[slot] = new RunLogLine(DateTime.UtcNow, level, message);
        if (count < Capacity) count++;
        else start = (start + 1) % Capacity;
        version++;
    }
}
