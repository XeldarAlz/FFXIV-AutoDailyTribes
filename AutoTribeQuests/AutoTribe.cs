using clib.TaskSystem;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System.Threading.Tasks;

namespace AutoTribeQuests;

// Main coroutine: travel to issuer -> accept dailies -> hand off to Questionable -> turn in.
public sealed class AutoTribe(TribeInfo tribe) : AutoCommon
{
    private readonly QuestionableIPC _questionable = new();

    protected override async Task Execute()
    {
        TribeData.RefreshLiveState(tribe);
        TribeData.ResolveIssuerLocation(tribe);

        ErrorIf(!tribe.Unlocked, $"{tribe.Name}: intro quest not complete");
        ErrorIf(!tribe.MeetsRankRequirement, $"{tribe.Name}: need rank {tribe.MinRankForDailies} (have {tribe.Rank})");
        ErrorIf(tribe.IssuerInstanceId == 0, $"{tribe.Name}: failed to resolve issuer NPC instance");
        ErrorIf(!_questionable.IsAvailable, "Questionable plugin not installed/enabled");

        var remainingToAccept = Math.Min(3 - tribe.AlreadyAcceptedToday.Length, tribe.DailyAllowanceLeft);
        if (remainingToAccept <= 0 && tribe.AlreadyAcceptedToday.Length == 0)
        {
            Status = "Nothing to do";
            return;
        }

        var accepted = new List<uint>(tribe.AlreadyAcceptedToday);

        if (remainingToAccept > 0)
        {
            // 1. Travel
            Status = $"Travelling to {tribe.Name}";
            await MoveTo(tribe.IssuerTerritoryId, tribe.IssuerLocation,
                MovementConfig.InteractRange,
                allowTeleportIfFaster: true);
            await Dismount();

            // 2. Open the daily menu
            Status = $"Talking to {tribe.Name} issuer";
            // clib's InteractWith takes an IGameObject; we use ours with raw instance id.
            ErrorIf(!Game.InteractWith(tribe.IssuerInstanceId), "Failed to interact with issuer NPC");
            await WaitUntilSkipping(
                () => Game.IsSelectIconStringActive() || Game.IsSelectStringActive(),
                "WaitForIssuerMenu",
                UiSkipOptions.Talk);

            // Some tribes hide the daily list behind a SelectString entry option first.
            if (Game.IsSelectStringActive())
            {
                clib.Extensions.ClientStructs.Addons.AddonSelectString.Select(tribe.IssuerSelectStringIndex);
                await WaitUntil(Game.IsSelectIconStringActive, "WaitForDailyList");
            }

            // 3. Accept up to `remainingToAccept` quests
            // We accept by index. We assume the addon lists "[Accept Quest] <name>" rows first,
            // but verify per-tribe — some tribes show extra option rows. For Amalj'aa it's plain.
            for (int i = 0; i < remainingToAccept; i++)
            {
                if (tribe.DailyAllowanceLeft <= 0) break;
                if (!Game.IsSelectIconStringActive())
                {
                    Log("Issuer menu closed early — assuming allowance hit or no more quests offered");
                    break;
                }

                var optionCount = Game.CountSelectIconStringOptions();
                if (optionCount <= 1) // last row is usually "Quit"
                {
                    Log("No more quests offered");
                    break;
                }

                Status = $"Accepting quest {i + 1}/{remainingToAccept}";
                Game.SelectIconString(0); // pick first accept row; refine per-tribe later

                // Game flow after picking: Talk -> JournalAccept -> Talk -> menu reopens
                await WaitUntilSkipping(
                    () => Game.IsJournalAcceptActive() || Game.IsSelectYesnoActive(),
                    "WaitForJournalAccept",
                    UiSkipOptions.Talk);

                if (Game.IsSelectYesnoActive())
                {
                    clib.Extensions.ClientStructs.Addons.AddonSelectYesno.Yes();
                    await WaitWhile(Game.IsSelectYesnoActive, "WaitYesNoClose");
                }
                else if (Game.IsJournalAcceptActive())
                {
                    Game.JournalAcceptConfirm();
                    await WaitWhile(Game.IsJournalAcceptActive, "WaitJournalClose");
                }

                // Wait for either the menu to reopen, or interaction to end
                await WaitUntilSkipping(
                    () => Game.IsSelectIconStringActive() || !Game.IsTalkInProgress() && !Game.IsJournalAcceptActive(),
                    "WaitMenuReopen",
                    UiSkipOptions.Talk);

                // After acceptance, refresh acceptedToday by re-reading the journal.
                TribeData.RefreshLiveState(tribe);
                if (tribe.AlreadyAcceptedToday.Length > accepted.Count)
                    accepted.Add(tribe.AlreadyAcceptedToday[^1]);
            }

            if (Game.IsSelectIconStringActive())
                Game.CancelSelectIconString();
        }

        ErrorIf(accepted.Count == 0, "No quests accepted and none in journal");

        // 4. Delegate to Questionable
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

        // 5. Turn-in fallback: if any of the accepted quests are still active in journal,
        // walk back to the issuer and progress dialogs. Most quests Questionable hands in
        // itself, but crafter tribes occasionally drop us back at the issuer.
        // TODO: implement turn-in fallback once we have a real tribe to test against.

        Status = "Done";
    }
}
