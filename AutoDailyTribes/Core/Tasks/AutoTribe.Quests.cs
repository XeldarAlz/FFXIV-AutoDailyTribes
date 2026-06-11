using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using clib.Extensions;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public sealed partial class AutoTribe
{
    private async Task<bool> EnsureCorrectJob()
    {
        if (JobSwitcher.CurrentJobSatisfies(tribe.Kind)) return true;

        var gearsetId = JobSwitcher.PickGearset(tribe, Plugin.Cfg);
        if (gearsetId < 0)
        {
            var hint = tribe.Kind switch
            {
                TribeKind.Crafter  => "a Disciple of the Hand (crafter) gearset",
                TribeKind.Gatherer => "a Miner or Botanist gearset (Fisher dailies can't be automated)",
                TribeKind.Mixed    => "a crafter, Miner, or Botanist gearset",
                TribeKind.Combat   => "a combat job gearset",
                _                  => "a suitable gearset",
            };
            Warning($"{tribe.Name}: no usable gearset — create {hint} in-game, then run again — skipping");
            return false;
        }

        var targetJob = JobSwitcher.GearsetClassJob(gearsetId);
        Status = $"Switching to gearset {gearsetId}";
        Diag($"{tribe.Name}: equipping gearset {gearsetId} (target ClassJob {targetJob})");

        if (!await WaitUntilTimed(JobSwitchAllowed, AdtConstants.JobSwitchReadyMs, $"{tribe.Name} job-switch window"))
            Diag($"{tribe.Name}: job-switch window never fully cleared; attempting swap anyway");

        var deadline = Environment.TickCount64 + AdtConstants.JobSwitchConfirmMs;
        var lastDispatchMs = 0L;
        while (Environment.TickCount64 < deadline)
        {
            if (JobSwitcher.CurrentClassJob() == targetJob) return true;

            if (Environment.TickCount64 - lastDispatchMs >= AdtConstants.JobSwitchRedispatchMs && JobSwitchAllowed())
            {
                if (!JobSwitcher.EquipGearset(gearsetId))
                    Diag($"{tribe.Name}: EquipGearset rejected this pass (in combat / occupied / between areas); will retry");
                lastDispatchMs = Environment.TickCount64;
            }
            await NextFrame();
        }

        Warning($"{tribe.Name}: job did not switch to {targetJob} within time limit — skipping");
        return false;
    }

    private static bool JobSwitchAllowed()
        => !Svc.Condition[ConditionFlag.InCombat]
        && !Svc.Condition[ConditionFlag.Casting]
        && !Svc.Condition[ConditionFlag.Casting87]
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.BetweenAreas51]
        && !Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
        && !Svc.Condition[ConditionFlag.OccupiedInEvent]
        && !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
        && !Svc.Condition[ConditionFlag.Occupied]
        && !Svc.Condition[ConditionFlag.Occupied33]
        && !Svc.Condition[ConditionFlag.Occupied38]
        && !Svc.Condition[ConditionFlag.Occupied39];

    private async Task<ExitReason> DoAcceptPass()
    {
        var before = tribe.AcceptedTodayCount;
        var remaining = Math.Min(tribe.AcceptSlotsRemaining, tribe.DailyAllowanceLeft);

        Status = $"Talking to {tribe.Name} issuer";
        await AcceptDailies(remaining);
        TribeStateReader.Refresh(tribe);

        if (tribe.AcceptedTodayCount <= before)
        {
            acceptFailPasses++;
            if (acceptFailPasses >= MaxAcceptFailPasses)
            {
                Warning($"{tribe.Name}: could not accept dailies at the issuer (two passes made no progress); skipping");
                return ExitReason.Quit;
            }
            arrivedAtIssuer = false;
        }
        else
        {
            acceptFailPasses = 0;
        }
        return ExitReason.Continue;
    }

    private async Task AcceptDailies(int slotsToFill)
    {
        var startCount = tribe.AcceptedTodayCount;
        var targetCount = Math.Min(startCount + slotsToFill, AdtConstants.MaxAcceptsPerTribe);
        Diag($"{tribe.Name}: AcceptDailies {startCount} -> {targetCount} (+{slotsToFill})");

        const int maxFrames = 3600;                // ~60s @ ~60fps
        const int stateRefreshIntervalFrames = 20;
        const int reInteractGapFrames = 180;       // 3s @ ~60fps
        const int addonSettleFrames = 10;
        const int talkAdvanceFrames = 3;
        var frame = 0;
        var lastInteractFrame = -100;

        while (frame < maxFrames)
        {
            await NextFrame();
            frame++;

            if (frame % stateRefreshIntervalFrames == 0)
            {
                TribeStateReader.Refresh(tribe);
                Status = $"Accepted {tribe.AcceptedTodayCount}/{targetCount}";
                if (tribe.AcceptedTodayCount >= targetCount)
                {
                    Diag($"{tribe.Name}: AcceptDailies reached {tribe.AcceptedTodayCount}/{targetCount}");
                    if (AddonProbes.SelectIconStringActive()) AddonInteractions.SelectIconStringCancel();
                    return;
                }
            }

            if (AddonProbes.SelectYesnoActive())
            {
                AddonSelectYesno.Yes();
                await PauseFrames(addonSettleFrames);
                continue;
            }
            if (AddonProbes.JournalAcceptActive())
            {
                AddonInteractions.JournalAcceptConfirm();
                await PauseFrames(addonSettleFrames);
                continue;
            }
            if (AddonProbes.TalkActive())
            {
                AddonInteractions.ProgressTalk();
                await PauseFrames(talkAdvanceFrames);
                continue;
            }
            if (AddonProbes.SelectIconStringActive())
            {
                AddonInteractions.SelectIconStringPick(0);
                await PauseFrames(addonSettleFrames);
                continue;
            }
            if (AddonProbes.SelectStringActive())
            {
                AddonSelectString.Select(tribe.IssuerSelectStringIndex);
                await PauseFrames(addonSettleFrames);
                continue;
            }

            var inConversation = Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
                              || Svc.Condition[ConditionFlag.OccupiedInEvent];

            if (!inConversation
                && frame - lastInteractFrame >= reInteractGapFrames
                && !Svc.Condition[ConditionFlag.Jumping]
                && !Svc.Condition[ConditionFlag.Jumping61]
                && !Svc.Condition[ConditionFlag.Casting])
            {
                var ok = AddonInteractions.InteractWith(tribe.IssuerInstanceId);
                Diag($"{tribe.Name}: frame {frame} re-interacting -> triggered={ok}");
                lastInteractFrame = frame;
            }
        }

        Warning($"{tribe.Name}: AcceptDailies timed out at {tribe.AcceptedTodayCount}/{targetCount} after {maxFrames} frames");
        if (AddonProbes.SelectIconStringActive()) AddonInteractions.SelectIconStringCancel();
    }

    private async Task PauseFrames(int n)
    {
        for (var i = 0; i < n; i++) await NextFrame();
    }

    private async Task DelegateToQuestionable(uint[] accepted)
    {
        if (accepted.Length == 0) return;

        // Fisher dailies can't be automated (Questionable has no fishing support), so never
        // delegate them — flag them for manual completion instead of stalling on them.
        var fishing     = Array.FindAll(accepted, TribeStateReader.RequiresFisher);
        var deliverable = Array.FindAll(accepted, q => !TribeStateReader.RequiresFisher(q));

        if (fishing.Length > 0)
            Warning($"{tribe.Name}: {fishing.Length} fishing daily(ies) can't be automated — complete manually: " +
                    string.Join(", ", Array.ConvertAll(fishing, QuestName)));

        if (deliverable.Length == 0)
        {
            Diag($"{tribe.Name}: only fishing dailies in journal — nothing to delegate");
            return;
        }

        var active  = new List<uint>(deliverable); // quests still worth delegating
        var skipped = new List<uint>();            // quests Questionable couldn't finish

        void PushPriority(uint first)
        {
            questionable.ClearQuestPriority();
            foreach (var quest in active)
                questionable.AddQuestPriority(quest);
            if (!questionable.StartQuest(first))
                Warning($"{tribe.Name}: Questionable.StartQuest was rejected");
        }

        Diag($"{tribe.Name}: handing {active.Count} quest(s) to Questionable: " +
             string.Join(", ", active.ConvertAll(q => $"{q:X} ({QuestName(q)})")));
        PushPriority(active[0]);

        var deadline = Environment.TickCount64 + AdtConstants.QuestCompleteTimeoutMs;
        long? idleSinceMs = null;
        var restarts = 0;

        // Progress = a quest turned in, or Questionable switched to a different quest.
        var lastPending = active.Count;
        var lastCurrentId = questionable.CurrentQuestId();
        var progressSinceMs = Environment.TickCount64;

        try
        {
            while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
            {
                var pending = active.FindAll(questionable.IsQuestAccepted);
                if (pending.Count == 0)
                {
                    if (skipped.Count == 0) Diag($"{tribe.Name}: all {deliverable.Length} delegated quest(s) turned in");
                    else Warning($"{tribe.Name}: {skipped.Count} quest(s) couldn't be completed (stuck or unsupported step) — moving on");
                    return;
                }

                var done = deliverable.Length - pending.Count - skipped.Count;
                Status = $"Questionable: {done}/{deliverable.Length} done";

                var now = Environment.TickCount64;
                var currentId = questionable.CurrentQuestId();
                if (pending.Count < lastPending || currentId != lastCurrentId)
                {
                    lastPending = pending.Count;
                    lastCurrentId = currentId;
                    progressSinceMs = now;
                }

                // No turn-in and no quest change for too long → blame the active quest, drop it,
                // and keep going with the rest. Fires whether Questionable is busy-wedged (e.g. an
                // unautomatable fishing step) or idle-looping on a quest it can't run — in the
                // latter case an undroppable quest at the head of the list would otherwise starve
                // the doable ones, since Questionable always picks the first accepted quest.
                if (now - progressSinceMs >= AdtConstants.QuestStuckMs)
                {
                    var stuck = currentId is null ? 0u : pending.Find(q => QuestionableIPC.Compact(q) == currentId);
                    if (stuck == 0u) stuck = pending[0];

                    Warning($"{tribe.Name}: Questionable made no progress on {stuck:X} ({QuestName(stuck)}) for {AdtConstants.QuestStuckMs / 1000}s — skipping it");
                    active.Remove(stuck);
                    skipped.Add(stuck);

                    var remaining = active.FindAll(questionable.IsQuestAccepted);
                    if (remaining.Count == 0)
                    {
                        Warning($"{tribe.Name}: {skipped.Count} quest(s) couldn't be completed (stuck or unsupported step) — moving on");
                        return;
                    }

                    Diag($"{tribe.Name}: re-prioritising {remaining.Count} remaining quest(s)");
                    PushPriority(remaining[0]);
                    lastPending = remaining.Count;
                    lastCurrentId = null;
                    progressSinceMs = now;
                    idleSinceMs = null;
                    restarts = 0;
                    await NextFrame(100);
                    continue;
                }

                // Secondary nudge: if Questionable goes idle, restart the batch a few times to get
                // it moving again; the stall-drop above is the real terminator if restarts don't help.
                if (questionable.IsRunning())
                {
                    idleSinceMs = null;
                }
                else
                {
                    idleSinceMs ??= now;
                    if (now - idleSinceMs.Value >= AdtConstants.QuestIdleRestartMs && restarts < AdtConstants.MaxQuestRestarts)
                    {
                        restarts++;
                        Diag($"{tribe.Name}: Questionable went idle ({pending.Count} left) — restarting [{restarts}/{AdtConstants.MaxQuestRestarts}]");
                        PushPriority(pending[0]);
                        idleSinceMs = null;
                    }
                }

                await NextFrame(100);
            }

            Warning($"{tribe.Name}: Questionable did not finish all quests within time limit — moving on");
        }
        finally
        {
            questionable.Stop("ADT: tribe complete");
            questionable.ClearQuestPriority();
        }
    }
}
