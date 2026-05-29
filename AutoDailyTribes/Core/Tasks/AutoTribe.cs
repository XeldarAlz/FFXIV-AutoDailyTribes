using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using clib.Extensions;
using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public sealed class AutoTribe(TribeInfo tribe) : AutoCommon
{
    private readonly QuestionableIPC _questionable = new();

    public TribeInfo Tribe => tribe;

    protected override async Task Execute()
    {
        Svc.Log.Info($"[ADT] Starting {tribe.Name}");
        try
        {
            await ExecuteInner();
            Svc.Log.Info($"[ADT] {tribe.Name}: done.");
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var lastBracket = msg.LastIndexOf("] ");
            if (lastBracket >= 0) msg = msg[(lastBracket + 2)..];
            Svc.Log.Error($"[ADT] {tribe.Name} stopped: {msg}");
            throw;
        }
    }

    private async Task ExecuteInner()
    {
        TribeStateReader.Refresh(tribe);
        IssuerResolver.Resolve(tribe);

        ErrorIf(!tribe.Unlocked, $"{tribe.Name}: not unlocked — complete the intro quest in-game first");
        ErrorIf(!tribe.MeetsRankRequirement, $"{tribe.Name}: need rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        ErrorIf(tribe.IssuerInstanceId == 0, $"{tribe.Name}: BaseId placeholder — run /adt target next to the issuer to capture the real one");
        ErrorIf(!_questionable.IsAvailable, "Questionable plugin not installed/enabled");

        Diag($"State: rank={tribe.Rank}/{tribe.MinRankForDailies}, allowance={tribe.DailyAllowanceLeft}, acceptedToday={tribe.AcceptedTodayCount}, inProgress={tribe.InProgressQuestIds.Length}, currentTerritory={Svc.ClientState.TerritoryType}, issuerTerritory={tribe.IssuerTerritoryId}");

        if (Svc.ClientState.TerritoryType != tribe.IssuerTerritoryId)
        {
            Status = $"Teleporting to {tribe.Name}";
            Diag($"Teleporting to territory {tribe.IssuerTerritoryId} (current: {Svc.ClientState.TerritoryType})");
            await TeleportTo(tribe.IssuerTerritoryId, tribe.IssuerLocation, allowSameZoneTeleport: false);
            await WaitUntilTerritory(tribe.IssuerTerritoryId);
            Diag($"Arrived at territory {Svc.ClientState.TerritoryType}");
        }

        var remainingToAccept = Math.Min(tribe.AcceptSlotsRemaining, tribe.DailyAllowanceLeft);
        if (remainingToAccept <= 0 && !tribe.HasInProgressQuests)
        {
            Status = "Nothing to do";
            Diag("Nothing to do — no daily slots remaining and no in-progress quests in journal");
            return;
        }

        if (!await EnsureCorrectJob()) return;

        if (remainingToAccept > 0)
        {
            await TravelToIssuer();
            Status = $"Talking to {tribe.Name} issuer";
            await AcceptDailies(remainingToAccept);
        }

        TribeStateReader.Refresh(tribe);
        var accepted = tribe.InProgressQuestIds;
        ErrorIf(accepted.Length == 0, "No quests accepted and none in journal");

        await DelegateToQuestionable(accepted);

        Status = "Done";
    }

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
        Diag($"Equipping gearset {gearsetId} (target ClassJob {targetJob})");
        if (!JobSwitcher.EquipGearset(gearsetId))
        {
            Warning($"{tribe.Name}: EquipGearset rejected (in combat / occupied / between areas) — skipping");
            return false;
        }

        // EquipGearset returning 0 only means the request was dispatched — the swap is async.
        // Confirm it actually landed; if it never does, skip rather than run the tribe on the wrong job.
        for (var f = 0; f < AdtConstants.GearsetSwitchFrames; f++)
        {
            await NextFrame();
            if (JobSwitcher.CurrentClassJob() == targetJob) return true;
        }

        Warning($"{tribe.Name}: job did not switch to {targetJob} within time limit — skipping");
        return false;
    }

    private async Task TravelToIssuer()
    {
        Status = $"Travelling to {tribe.Name}";
        var config = MovementConfig.Everything.WithTolerance(3f);
        await MoveTo(tribe.IssuerTerritoryId, tribe.IssuerLocation,
            config,
            allowTeleportIfFaster: true);
        await Dismount();
    }

    private async Task AcceptDailies(int slotsToFill)
    {
        var startCount = tribe.AcceptedTodayCount;
        var targetCount = Math.Min(startCount + slotsToFill, AdtConstants.MaxAcceptsPerTribe);
        Diag($"AcceptDailies: {startCount} → {targetCount} (+{slotsToFill})");

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
                    Diag($"AcceptDailies: reached {tribe.AcceptedTodayCount}/{targetCount}");
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
            // OccupiedInQuestEvent / OccupiedInEvent are what FFXIV sets while an NPC dialog is
            // active; spamming InteractWith during that closes the dialog we just opened. This is
            // the same gate Questionable uses (DoInteract waits on these condition flags).
            var inConversation = Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
                              || Svc.Condition[ConditionFlag.OccupiedInEvent];

            if (!inConversation
                && frame - lastInteractFrame >= 180  // 3s @ ~60fps — Questionable's _continueAt is 0.5s but we add slack for the dialog to actually surface
                && !Svc.Condition[ConditionFlag.Jumping]
                && !Svc.Condition[ConditionFlag.Jumping61]
                && !Svc.Condition[ConditionFlag.Casting])
            {
                var ok = AddonInteractions.InteractWith(tribe.IssuerInstanceId);
                Diag($"Frame {frame}: re-interacting → triggered={ok}");
                lastInteractFrame = frame;
            }
        }

        Warning($"AcceptDailies: timed out at {tribe.AcceptedTodayCount}/{targetCount} after {maxFrames} frames");
        if (AddonProbes.SelectIconStringActive()) AddonInteractions.SelectIconStringCancel();
    }

    private async Task PauseFrames(int n)
    {
        for (var i = 0; i < n; i++) await NextFrame();
    }

    private void Diag(string msg)
    {
        Svc.Log.Info($"[{tribe.Name}] {msg}");
    }

    private async Task DelegateToQuestionable(uint[] accepted)
    {
        for (var i = 0; i < accepted.Length; i++)
        {
            var quest = accepted[i];
            if (_questionable.IsQuestComplete(quest)) continue;

            Status = $"Questionable: quest {i + 1}/{accepted.Length}";
            Diag($"StartSingleQuest {quest:X} ({QuestName(quest)})");
            ErrorIf(!_questionable.StartSingleQuest(quest),
                $"Questionable.StartSingleQuest rejected quest {quest:X} ({QuestName(quest)})");

            await RunQuestionableQuest(quest);
        }
    }

    // StartSingleQuest sets Questionable's automation type synchronously, so IsRunning should flip
    // true within a few frames. We wait for it to engage (bounded), then wait for it to finish
    // (bounded), so neither a no-op start nor a hung quest can lock the tribe forever.
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
