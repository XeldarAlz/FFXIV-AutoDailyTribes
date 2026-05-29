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
        if (!JobSwitcher.EquipGearset(gearsetId))
        {
            Warning($"{tribe.Name}: EquipGearset rejected (in combat / occupied / between areas) — skipping");
            return false;
        }

        // EquipGearset returning 0 only means the request was dispatched — the swap is async. Confirm it
        // actually landed; if it never does, skip rather than run the tribe on the wrong job.
        for (var f = 0; f < AdtConstants.GearsetSwitchFrames; f++)
        {
            await NextFrame();
            if (JobSwitcher.CurrentClassJob() == targetJob) return true;
        }

        Warning($"{tribe.Name}: job did not switch to {targetJob} within time limit — skipping");
        return false;
    }

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

    private async Task DelegateToQuestionable(uint[] accepted)
    {
        for (var i = 0; i < accepted.Length; i++)
        {
            var quest = accepted[i];
            if (_questionable.IsQuestComplete(quest)) continue;

            Status = $"Questionable: quest {i + 1}/{accepted.Length}";
            Diag($"{tribe.Name}: StartSingleQuest {quest:X} ({QuestName(quest)})");
            ErrorIf(!_questionable.StartSingleQuest(quest),
                $"Questionable.StartSingleQuest rejected quest {quest:X} ({QuestName(quest)})");

            await RunQuestionableQuest(quest);
        }
    }

    // StartSingleQuest sets Questionable's automation type synchronously, so IsRunning should flip true
    // within a few frames. We wait for it to engage (bounded), then wait for it to finish (bounded), so
    // neither a no-op start nor a hung quest can lock the tribe forever.
    private async Task RunQuestionableQuest(uint quest)
    {
        var startFrame = 0;
        while (startFrame < AdtConstants.QuestStartFrames && !_questionable.IsRunning())
        {
            if (_questionable.IsQuestComplete(quest)) return;
            await NextFrame();
            startFrame++;
        }

        if (!_questionable.IsRunning())
        {
            Warning($"{tribe.Name}: Questionable never engaged on {quest:X} ({QuestName(quest)}) — skipping");
            return;
        }

        var runFrame = 0;
        while (_questionable.IsRunning())
        {
            ErrorIf(runFrame++ >= AdtConstants.QuestRunFrames,
                $"Questionable run on {quest:X} ({QuestName(quest)}) exceeded time limit");
            await NextFrame();
        }
    }
}
