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

        questionable.ClearQuestPriority();
        foreach (var quest in accepted)
            questionable.AddQuestPriority(quest);

        Diag($"{tribe.Name}: handing {accepted.Length} quest(s) to Questionable: " +
             string.Join(", ", Array.ConvertAll(accepted, q => $"{q:X} ({QuestName(q)})")));
        if (!questionable.StartQuest(accepted[0]))
            Warning($"{tribe.Name}: Questionable.StartQuest was rejected");

        var deadline = Environment.TickCount64 + AdtConstants.QuestCompleteTimeoutMs;
        long? idleSinceMs = null;
        var restarts = 0;

        try
        {
            while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
            {
                var pending = Array.FindAll(accepted, questionable.IsQuestAccepted);
                if (pending.Length == 0)
                {
                    Diag($"{tribe.Name}: all {accepted.Length} quest(s) turned in");
                    return;
                }
                Status = $"Questionable: {accepted.Length - pending.Length}/{accepted.Length} done";

                if (questionable.IsRunning())
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
                        questionable.StartQuest(pending[0]);
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
