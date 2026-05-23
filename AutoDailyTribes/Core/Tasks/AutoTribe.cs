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
        Svc.Chat.Print($"[ADT debug] Entering AcceptDailies: addons=[{ActiveAddons()}]");

        for (int i = 0; i < remaining; i++)
        {
            if (tribe.DailyAllowanceLeft <= 0) break;
            if (!AddonProbes.SelectIconStringActive())
            {
                Svc.Chat.Print($"[ADT debug] Loop {i + 1}: SelectIconString not active — addons=[{ActiveAddons()}]. Breaking.");
                break;
            }

            Status = $"Accepting quest {i + 1}/{remaining}";
            var beforeAccepted = tribe.AlreadyAcceptedToday.Length;
            Svc.Chat.Print($"[ADT debug] Loop {i + 1}: picking SelectIconString[0]  (journal: {beforeAccepted} tribe quest(s) accepted today)");
            AddonInteractions.SelectIconStringPick(0);

            // Give the game a few frames to react.
            for (var f = 0; f < 30; f++) await NextFrame();
            Svc.Chat.Print($"[ADT debug] After pick: addons=[{ActiveAddons()}]");

            await WaitUntilSkipping(
                () => AddonProbes.JournalAcceptActive() || AddonProbes.SelectYesnoActive() || !AddonProbes.SelectIconStringActive(),
                "WaitForAcceptPrompt",
                UiSkipOptions.Talk);

            var afterWait = ActiveAddons();
            Svc.Chat.Print($"[ADT debug] After WaitForAcceptPrompt: addons=[{afterWait}]");

            if (AddonProbes.SelectYesnoActive())
            {
                Svc.Chat.Print("[ADT debug] SelectYesno → clicking Yes");
                AddonSelectYesno.Yes();
                await WaitWhile(AddonProbes.SelectYesnoActive, "WaitYesNoClose");
            }
            else if (AddonProbes.JournalAcceptActive())
            {
                Svc.Chat.Print("[ADT debug] JournalAccept → confirm");
                AddonInteractions.JournalAcceptConfirm();
                await WaitWhile(AddonProbes.JournalAcceptActive, "WaitJournalClose");
            }
            else
            {
                Svc.Chat.Print("[ADT debug] Neither JournalAccept nor SelectYesno appeared — pick callback shape likely wrong");
            }

            await WaitUntilSkipping(
                () => AddonProbes.SelectIconStringActive() || (!AddonProbes.TalkActive() && !AddonProbes.JournalAcceptActive()),
                "WaitMenuReopen",
                UiSkipOptions.Talk);

            TribeStateReader.Refresh(tribe);
            var afterAccepted = tribe.AlreadyAcceptedToday.Length;
            Svc.Chat.Print($"[ADT debug] After refresh: journal has {afterAccepted} tribe quest(s) for this tribe");
            if (afterAccepted > accepted.Count)
                accepted.Add(tribe.AlreadyAcceptedToday[^1]);
        }

        if (AddonProbes.SelectIconStringActive())
            AddonInteractions.SelectIconStringCancel();
    }

    private static string ActiveAddons()
    {
        string[] watch = ["SelectString", "SelectIconString", "JournalAccept", "JournalDetail", "JournalResult", "Talk", "SelectYesno", "Request", "_Notification"];
        return string.Join(", ", watch.Where(AddonProbes.Ready));
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
