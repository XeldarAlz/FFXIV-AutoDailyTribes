// Minimal game boundary for exercising the real controller without loading Dalamud.
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes
{
    internal static class Plugin
    {
        internal static Configuration Cfg { get; } = new();
    }
    internal sealed class Configuration
    {
        public string PostRunCommands => "/echo done";
    }
}

namespace AutoDailyTribes.Core
{
    internal static class RunLog
    {
        public static List<Exception> Errors { get; } = [];
        public static List<string> Warnings { get; } = [];
        public static void Info(string message) { }
        public static void Error(Exception exception, string message) => Errors.Add(exception);
        public static void Warning(Exception exception, string message) => Errors.Add(exception);
        public static void Warning(string message) => Warnings.Add(message);
    }
}

namespace AutoDailyTribes.Core.Tribes
{
    public sealed record TribeInfo(string Name)
    {
        public string? IconFile => null;
    }
}

namespace AutoDailyTribes.Core.External
{
    internal static class ExternalPlugins
    {
        public sealed record Entry(bool Required, string DisplayName);
        public static bool Ready = true;
        public static IEnumerable<int> All => [1];
        public static Dictionary<int, Entry> Catalog { get; } = new() { [1] = new(true, "Questionable") };
        public static bool AllRequiredInstalled() => Ready;
        public static bool IsInstalled(int plugin) => Ready;
    }
    internal static class QuestionableSettings
    {
        public const string SettingDisplayName = "Prevent quest completion";
        public static bool Borrowed;
        public static bool ThrowOnBorrow;
        public static int BorrowCalls;
        public static bool TryBorrowQuestCompletion(out string failure)
        {
            BorrowCalls++;
            Borrowed = true;
            if (ThrowOnBorrow) throw new InvalidOperationException("settings failed");
            failure = "";
            return true;
        }
        public static void RestoreQuestCompletion() => Borrowed = false;
    }
}

namespace AutoDailyTribes.Core.Game
{
    internal static class ChatCommands
    {
        public static int Calls;
        public static void Dispatch(string commands) => Calls++;
    }
}

namespace AutoDailyTribes.Core.Tasks
{
    internal sealed class AutoTribe
    {
        public static string? ThrowForTribe;
        public static int Created;
        public Exception? Fault { get; set; }
        public bool Cancelled { get; private set; }
        public string Status => "Working";
        public TribeInfo Tribe { get; }
        public TribeRunProgress Progress { get; }
        public AutoTribe(TribeInfo tribe, TribeRunProgress progress)
        {
            Created++;
            if (tribe.Name == ThrowForTribe) throw new InvalidOperationException("constructor failed");
            Tribe = tribe;
            Progress = progress;
        }
        public void Cancel() => Cancelled = true;
    }
}

namespace clib.Services
{
    internal static class Svc
    {
        public static FakeAutomation Automation = new();
    }
    internal sealed class FakeAutomation
    {
        public AutoTribe? CurrentTask { get; private set; }
        public AutoTribe? LastTask;
        public Action? Completion;
        public bool ThrowOnStart;
        public bool ThrowOnStop;
        public bool Running => CurrentTask is not null;
        public void Start(AutoTribe task, Action? OnCompleted = null)
        {
            LastTask = task;
            if (ThrowOnStart) throw new InvalidOperationException("start failed");
            CurrentTask = task;
            Completion = OnCompleted;
        }
        public void Stop()
        {
            if (ThrowOnStop) throw new InvalidOperationException("stop failed");
            CurrentTask?.Cancel();
            CurrentTask = null;
        }
        public void Complete(Exception? fault = null, RunOutcome outcome = RunOutcome.Completed, string detail = "done", bool recordOutcome = true)
        {
            var task = CurrentTask!;
            task.Fault = fault;
            if (recordOutcome)
                task.Progress.LogOutcome(task.Tribe, fault is null ? outcome : RunOutcome.Stopped, fault?.Message ?? detail);
            CurrentTask = null;
            Completion!(); // clib calls this even after a fault.
        }
    }
}

namespace ECommons.DalamudServices
{
    internal static class Svc
    {
        public static FakeChat Chat { get; } = new();
    }
    internal sealed class FakeChat
    {
        public int Errors;
        public List<string> Messages { get; } = [];
        public void PrintError(string message)
        {
            Errors++;
            Messages.Add(message);
        }
    }
}
