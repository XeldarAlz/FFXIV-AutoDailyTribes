using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using AutoDailyTribes.Core.Tribes;
using clib.Extensions;
using clib.TaskSystem;
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

        var remainingToAccept = Math.Min(tribe.AcceptSlotsRemaining, tribe.DailyAllowanceLeft);
        if (remainingToAccept <= 0 && tribe.AlreadyAcceptedToday.Length == 0)
        {
            Status = "Nothing to do";
            return;
        }

        var accepted = new List<uint>(tribe.AlreadyAcceptedToday);

        if (remainingToAccept > 0)
        {
            await TravelAndOpenMenu();
            await AcceptDailies(remainingToAccept, accepted);
        }

        ErrorIf(accepted.Count == 0, "No quests accepted and none in journal");

        await DelegateToQuestionable(accepted);

        Status = "Done";
    }

    private async Task TravelAndOpenMenu()
    {
        Status = $"Travelling to {tribe.Name}";
        await MoveTo(tribe.IssuerTerritoryId, tribe.IssuerLocation,
            MovementConfig.InteractRange,
            allowTeleportIfFaster: true);
        await Dismount();

        Status = $"Talking to {tribe.Name} issuer";
        Svc.Chat.Print($"[ADT debug] Interacting with issuer ({tribe.IssuerInstanceId:X})");
        ErrorIf(!AddonInteractions.InteractWith(tribe.IssuerInstanceId), "Failed to interact with issuer NPC");
        await WaitUntilSkipping(
            () => AddonProbes.SelectIconStringActive() || AddonProbes.SelectStringActive(),
            "WaitForIssuerMenu",
            UiSkipOptions.Talk);

        var firstMenu = AddonProbes.SelectIconStringActive() ? "SelectIconString"
                      : AddonProbes.SelectStringActive() ? "SelectString"
                      : "?";
        Svc.Chat.Print($"[ADT debug] First menu opened: {firstMenu}");

        if (AddonProbes.SelectStringActive())
        {
            Svc.Chat.Print($"[ADT debug] Picking SelectString[{tribe.IssuerSelectStringIndex}] as entry hop");
            AddonSelectString.Select(tribe.IssuerSelectStringIndex);
            await WaitUntil(AddonProbes.SelectIconStringActive, "WaitForDailyList");
            Svc.Chat.Print("[ADT debug] SelectIconString daily list now active");
        }
    }

    private async Task AcceptDailies(int remaining, List<uint> accepted)
    {
        for (int i = 0; i < remaining; i++)
        {
            if (tribe.DailyAllowanceLeft <= 0) break;
            var sisActive = AddonProbes.SelectIconStringActive();
            var optCount = AddonProbes.SelectIconStringOptionCount();
            Svc.Chat.Print($"[ADT debug] Loop {i + 1}/{remaining}: SelectIconString active={sisActive}, AtkValues[0].Int={optCount}");

            if (!sisActive)
            {
                Log("Issuer menu closed early — assuming allowance hit or no more offered");
                break;
            }
            if (optCount <= 1)
            {
                Log("No more quests offered");
                break;
            }

            Status = $"Accepting quest {i + 1}/{remaining}";
            Svc.Chat.Print($"[ADT debug] Picking SelectIconString[0]");
            AddonInteractions.SelectIconStringPick(0);

            await WaitUntilSkipping(
                () => AddonProbes.JournalAcceptActive() || AddonProbes.SelectYesnoActive(),
                "WaitForAcceptPrompt",
                UiSkipOptions.Talk);

            if (AddonProbes.SelectYesnoActive())
            {
                AddonSelectYesno.Yes();
                await WaitWhile(AddonProbes.SelectYesnoActive, "WaitYesNoClose");
            }
            else if (AddonProbes.JournalAcceptActive())
            {
                AddonInteractions.JournalAcceptConfirm();
                await WaitWhile(AddonProbes.JournalAcceptActive, "WaitJournalClose");
            }

            await WaitUntilSkipping(
                () => AddonProbes.SelectIconStringActive() || (!AddonProbes.TalkActive() && !AddonProbes.JournalAcceptActive()),
                "WaitMenuReopen",
                UiSkipOptions.Talk);

            TribeStateReader.Refresh(tribe);
            if (tribe.AlreadyAcceptedToday.Length > accepted.Count)
                accepted.Add(tribe.AlreadyAcceptedToday[^1]);
        }

        if (AddonProbes.SelectIconStringActive())
            AddonInteractions.SelectIconStringCancel();
    }

    private async Task DelegateToQuestionable(List<uint> accepted)
    {
        Status = $"Delegating {accepted.Count} quest(s) to Questionable";
        _questionable.ClearPriority();
        foreach (var q in accepted)
        {
            if (_questionable.IsQuestLocked(q))
            {
                Warning($"Questionable says quest {q} ({QuestName(q)}) is locked — skipping");
                continue;
            }
            _questionable.AddPriority(q);
        }
        _questionable.StartQuest(accepted[0]);
        await WaitUntilThenFalse(_questionable.IsRunning, "QuestionableRun");
    }
}
