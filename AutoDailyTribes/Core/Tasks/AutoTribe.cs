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
        Svc.Chat.Print($"[ADT] Starting {tribe.Name}…");
        try
        {
            await ExecuteInner();
            Svc.Chat.Print($"[ADT] {tribe.Name}: done.");
        }
        catch (Exception ex)
        {
            // Strip clib's "[AutoTribe] [scope] " prefix so the chat line is readable.
            var msg = ex.Message;
            var lastBracket = msg.LastIndexOf("] ");
            if (lastBracket >= 0) msg = msg[(lastBracket + 2)..];
            Svc.Chat.PrintError($"[ADT] {tribe.Name} stopped: {msg}");
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

        // FIRST: get on the right map regardless of quest state. User's expectation is unconditional.
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

        // Only walk to the NPC when we actually need to accept new quests — otherwise Questionable handles routing.
        if (remainingToAccept > 0)
        {
            await TravelToIssuer();
            Status = $"Talking to {tribe.Name} issuer";
            await AcceptDailies(remainingToAccept);
        }

        // Refresh once after accepts: whatever's in the journal now is what Questionable picks up.
        TribeStateReader.Refresh(tribe);
        var accepted = tribe.InProgressQuestIds;
        ErrorIf(accepted.Length == 0, "No quests accepted and none in journal");

        await DelegateToQuestionable(accepted);

        Status = "Done";
    }

    private async Task<bool> EnsureCorrectJob()
    {
        var target = JobSwitcher.ResolveTargetJob(tribe, Plugin.Cfg);
        if (target is null) return true;
        if (JobSwitcher.CurrentClassJob() == target.Value) return true;

        var gearsetId = JobSwitcher.FindGearsetForJob(target.Value);
        if (gearsetId < 0)
        {
            Warning($"{tribe.Name}: no gearset found for required job {target.Value} — skipping");
            return false;
        }

        Status = $"Switching to gearset {gearsetId}";
        Diag($"Equipping gearset {gearsetId} (target ClassJob {target.Value})");
        if (!JobSwitcher.EquipGearset(gearsetId))
        {
            Warning($"{tribe.Name}: EquipGearset rejected (in combat / mounted / etc.) — skipping");
            return false;
        }

        for (var f = 0; f < 120; f++)
        {
            await NextFrame();
            if (JobSwitcher.CurrentClassJob() == target.Value) break;
        }
        return true;
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

    // Drive whatever prompt is open and re-interact when nothing is — keep "pressing confirm"
    // until AcceptedTodayCount reaches the target. No per-quest state machine: the journal is
    // the source of truth, and Talk dialogue gets advanced inline.
    private async Task AcceptDailies(int slotsToFill)
    {
        var startCount = tribe.AcceptedTodayCount;
        var targetCount = Math.Min(startCount + slotsToFill, AdtConstants.MaxAcceptsPerTribe);
        Diag($"AcceptDailies: {startCount} → {targetCount} (+{slotsToFill})");

        const int maxFrames = 3600; // ~60s at 60fps; covers the slowest NPC chain comfortably
        var frame = 0;
        var lastInteractFrame = -100;

        while (frame < maxFrames)
        {
            await NextFrame();
            frame++;

            // Refresh journal periodically — once the count hits target we're done regardless
            // of which prompt is on screen.
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

            // Advance whichever prompt is up. Order: confirmation dialogs first, then Talk,
            // then the menus that actually pick a quest.
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
        Svc.Chat.Print($"[ADT debug] {msg}");
        Svc.Log.Info($"[{tribe.Name}] {msg}");
    }

    private async Task DelegateToQuestionable(uint[] accepted)
    {
        Status = $"Delegating {accepted.Length} quest(s) to Questionable";
        // Questionable picks up accepted tribe quests from the journal on its own — no need to seed its priority list.
        // StartQuest returning false means IPC rejected it — otherwise we'd hang in WaitUntilThenFalse waiting for IsRunning that never flips.
        ErrorIf(!_questionable.StartQuest(accepted[0]),
            $"Questionable.StartQuest rejected quest {accepted[0]:X} ({QuestName(accepted[0])})");
        await WaitUntilThenFalse(_questionable.IsRunning, "QuestionableRun");
    }
}
