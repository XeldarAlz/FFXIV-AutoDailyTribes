using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Tribes;
using clib.Services;
using System.Threading;

namespace AutoDailyTribes.Core.Tasks;

internal sealed class AutoTribeController
{
    private int runGeneration;
    private readonly TribeRunProgress progress = new();

    public bool Running => Svc.Automation.Running;
    public string Status => Svc.Automation.CurrentTask?.Status ?? "Idle";
    public TribeRunProgress Progress => progress;

    private static void Diag(string message) => RunLog.Info(message);

    private static bool RequiredPluginsReady()
    {
        if (ExternalPlugins.AllRequiredInstalled()) return true;

        var missing = string.Join(", ", ExternalPlugins.All
            .Where(p => ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p))
            .Select(p => ExternalPlugins.Catalog[p].DisplayName));
        Diag($"Start aborted: required plugins missing ({missing}).");
        ECommons.DalamudServices.Svc.Chat.PrintError($"[ADT] Cannot start — install all required plugins first: {missing}.");
        return false;
    }

    private static bool QuestCompletionReady()
    {
        if (QuestionableSettings.TryBorrowQuestCompletion(out var failure)) return true;

        Diag($"Start aborted: could not clear Questionable's '{QuestionableSettings.SettingDisplayName}' ({failure}).");
        ECommons.DalamudServices.Svc.Chat.PrintError(
            $"[ADT] Cannot start — could not check Questionable's '{QuestionableSettings.SettingDisplayName}' setting ({failure}). " +
            $"Turn it off in Questionable's Advanced settings, then run again.");
        return false;
    }

    public void RunAll(IEnumerable<TribeInfo> tribes)
    {
        if (Running) return;

        try
        {
            var list = tribes.ToList();
            if (list.Count == 0 || !RequiredPluginsReady()) return;
            if (!QuestCompletionReady())
            {
                QuestionableSettings.RestoreQuestCompletion();
                return;
            }

            var generation = Interlocked.Increment(ref runGeneration);
            progress.Begin(list);
            Diag($"RunAll: {list.Count} tribe(s) queued.");
            StartNext(new Queue<TribeInfo>(list), generation);
        }
        catch (Exception ex)
        {
            FailRun(ex, null);
        }
    }

    public void Stop()
    {
        Interlocked.Increment(ref runGeneration);
        try
        {
            Svc.Automation.Stop();
        }
        catch (Exception ex)
        {
            RunLog.Error(ex, "Could not stop automation cleanly");
        }
        finally
        {
            QuestionableSettings.RestoreQuestCompletion();
            progress.End();
        }
    }

    private void StartNext(Queue<TribeInfo> queue, int generation)
    {
        if (Volatile.Read(ref runGeneration) != generation)
        {
            // A newer run (or Stop) bumped the generation and now owns `progress`; leave it alone.
            Diag("RunAll: generation changed (Stop or new run); not continuing the batch.");
            return;
        }
        if (queue.Count == 0)
        {
            Diag("RunAll: all queued tribes processed.");
            var incomplete = progress.Log.Where(entry => entry.Outcome != RunOutcome.Completed).ToArray();
            var allCompleted = progress.Log.Count == progress.Total && incomplete.Length == 0;
            QuestionableSettings.RestoreQuestCompletion();
            progress.End();
            if (allCompleted)
            {
                ChatCommands.Dispatch(Plugin.Cfg.PostRunCommands);
            }
            else
            {
                var detail = incomplete.Length == 0
                    ? "Not every tribe reported a completed outcome."
                    : "Needs attention: " + string.Join("; ", incomplete.Select(entry => $"{entry.Name}: {entry.Detail}")) + ".";
                var message = $"Run finished with incomplete tribes. {detail} Post-run commands were skipped. Open /adt log for details.";
                RunLog.Warning(message);
                ECommons.DalamudServices.Svc.Chat.PrintError($"[ADT] {message}");
            }
            return;
        }

        var tribe = queue.Dequeue();
        Diag($"RunAll: starting {tribe.Name} ({queue.Count} remaining after this).");
        AutoTribe? task = null;
        try
        {
            task = new AutoTribe(tribe, progress);
            Svc.Automation.Start(task, OnCompleted: () =>
            {
                if (Volatile.Read(ref runGeneration) != generation) return;
                // clib invokes OnCompleted even when Execute faulted.
                if (task.Fault is { } fault)
                {
                    FailRun(fault, tribe, outcomeRecorded: true);
                    return;
                }
                progress.CompleteTribe();
                StartNext(queue, generation);
            });
        }
        catch (Exception ex)
        {
            // Start can throw before handing the task to Automation.
            try { task?.Cancel(); }
            catch (Exception cleanupEx) { RunLog.Warning(cleanupEx, "Could not cancel the failed task"); }
            FailRun(ex, tribe);
        }
    }

    private void FailRun(Exception exception, TribeInfo? tribe, bool outcomeRecorded = false)
    {
        RunLog.Error(exception, tribe is null ? "Could not start the run" : $"Run stopped at {tribe.Name}");
        if (tribe is not null && !outcomeRecorded)
            progress.LogOutcome(tribe, RunOutcome.Stopped, exception.Message);
        Stop();
        ECommons.DalamudServices.Svc.Chat.PrintError(
            "[ADT] The run stopped because of an error. Open /adt log for details.");
    }
}
