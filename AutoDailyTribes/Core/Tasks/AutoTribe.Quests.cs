using AutoDailyTribes.Core.Game;
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
            Warning($"{tribe.Name}: no {tribe.Kind} gearset found — create one for this tribe's job type — skipping");
            return false;
        }

        var targetJob = JobSwitcher.GearsetClassJob(gearsetId);
        Status = $"Switching to gearset {gearsetId}";
        Diag($"{tribe.Name}: equipping gearset {gearsetId} (target ClassJob {targetJob})");

        // A gearset swap is silently dropped while the game is mid-event / zoning / in combat — common in
        // the instant after the previous tribe's NPC dialog. Wait for a clear window before dispatching.
        if (!await WaitUntilTimed(JobSwitchAllowed, AdtConstants.JobSwitchReadyMs, $"{tribe.Name} job-switch window"))
            Diag($"{tribe.Name}: job-switch window never fully cleared; attempting swap anyway");

        // EquipGearset returning 0 only means the request was dispatched — the swap is async, and a single
        // request can be accepted-but-ignored during a transient lock. Re-dispatch on an interval and
        // confirm the job actually landed, bounded by wall clock so high frame rates can't shrink it.
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
            if (acceptFailPasses >= 2)
            {
                Warning($"{tribe.Name}: could not accept dailies at the issuer (two passes made no progress); skipping");
                return ExitReason.Quit;
            }
            // Re-path to the issuer in case we drifted out of interaction range before the next pass.
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

        const int maxFrames = 3600;
        var frame = 0;
        var lastInteractFrame = -100;

        while (frame < maxFrames)
        {
            await NextFrame();
            frame++;

            if (frame % 20 == 0)
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
                await PauseFrames(10);
                continue;
            }
            if (AddonProbes.JournalAcceptActive())
            {
                AddonInteractions.JournalAcceptConfirm();
                await PauseFrames(10);
                continue;
            }
            if (AddonProbes.TalkActive())
            {
                AddonInteractions.ProgressTalk();
                await PauseFrames(3);
                continue;
            }
            if (AddonProbes.SelectIconStringActive())
            {
                AddonInteractions.SelectIconStringPick(0);
                await PauseFrames(10);
                continue;
            }
            if (AddonProbes.SelectStringActive())
            {
                AddonSelectString.Select(tribe.IssuerSelectStringIndex);
                await PauseFrames(10);
                continue;
            }

            // Nothing on screen — re-interact, but only if we're not already in a conversation.
            // OccupiedInQuestEvent / OccupiedInEvent are what FFXIV sets while an NPC dialog is active;
            // spamming InteractWith during that closes the dialog we just opened. Same gate Questionable uses.
            var inConversation = Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
                              || Svc.Condition[ConditionFlag.OccupiedInEvent];

            if (!inConversation
                && frame - lastInteractFrame >= 180  // 3s @ ~60fps — slack for the dialog to surface
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

    // Hand the tribe's accepted dailies to Questionable as a batch and let it run them all to completion
    // (objectives + turn-in) in one Automatic session. We pin the quests to Questionable's priority list so
    // it stays on THEM rather than wandering onto the player's MSQ, then poll until none are still accepted
    // (all turned in) and Stop. Completion is judged per-quest by IsQuestAccepted leaving the journal — NOT
    // by IsRunning, which is the wrong signal (it merely reflects "automation engaged or tasks queued").
    private async Task DelegateToQuestionable(uint[] accepted)
    {
        if (accepted.Length == 0) return;

        _questionable.ClearQuestPriority();
        foreach (var quest in accepted)
            _questionable.AddQuestPriority(quest);

        Diag($"{tribe.Name}: handing {accepted.Length} quest(s) to Questionable: " +
             string.Join(", ", Array.ConvertAll(accepted, q => $"{q:X} ({QuestName(q)})")));
        if (!_questionable.StartQuest(accepted[0]))
            Warning($"{tribe.Name}: Questionable.StartQuest was rejected");

        var deadline = Environment.TickCount64 + AdtConstants.QuestCompleteTimeoutMs;
        long? idleSinceMs = null;
        var restarts = 0;

        try
        {
            while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
            {
                var pending = Array.FindAll(accepted, _questionable.IsQuestAccepted);
                if (pending.Length == 0)
                {
                    Diag($"{tribe.Name}: all {accepted.Length} quest(s) turned in");
                    return;
                }
                Status = $"Questionable: {accepted.Length - pending.Length}/{accepted.Length} done";

                // IsRunning stays true the whole time Questionable is engaged (Automatic mode, or tasks
                // queued) — including the gaps between our quests. It only goes false if Questionable
                // actually stopped (reverted to Manual / ran out of quests). Treat a sustained-idle window
                // as a stall and restart, bounded, so a hiccup recovers but a dead quest can't loop forever.
                if (_questionable.IsRunning())
                {
                    idleSinceMs = null;
                }
                else
                {
                    var now = Environment.TickCount64;
                    idleSinceMs ??= now;
                    if (now - idleSinceMs.Value >= AdtConstants.QuestIdleRestartMs)
                    {
                        if (restarts >= AdtConstants.MaxQuestRestarts)
                        {
                            Warning($"{tribe.Name}: Questionable idle with {pending.Length} quest(s) left after {restarts} restarts — giving up");
                            return;
                        }
                        restarts++;
                        Diag($"{tribe.Name}: Questionable went idle ({pending.Length} left) — restarting [{restarts}/{AdtConstants.MaxQuestRestarts}]");
                        _questionable.StartQuest(pending[0]);
                        idleSinceMs = null;
                    }
                }

                await NextFrame(100);
            }

            Warning($"{tribe.Name}: Questionable did not finish all quests within time limit — moving on");
        }
        finally
        {
            // Always hand the wheel back: stop the Automatic session so Questionable can't roll onto the
            // player's MSQ after our quests, and clear the priority entries we added.
            _questionable.Stop("ADT: tribe complete");
            _questionable.ClearQuestPriority();
        }
    }
}
