using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AutoTribeQuests.Core.Tribes;

// Pulls live tribe state — unlock, rank, daily allowance, accepted-today set —
// off the in-memory game structs and into the TribeInfo.
//
// Cheap enough to call every frame in the Draw() loop.
internal static unsafe class TribeStateReader
{
    public static void Refresh(TribeInfo tribe)
    {
        tribe.Unlocked = QuestManager.IsQuestComplete(tribe.UnlockQuestId);

        // TODO: rank + reputation from PlayerState.BeastReputation
        // TODO: scan QuestManager.NormalQuests for accepted dailies in this tribe's BeastTribeQuest subrows
        // TODO: read global daily allowance counter
        tribe.Rank = 0;
        tribe.RepCur = 0;
        tribe.RepMax = 0;
        tribe.AlreadyAcceptedToday = [];
        tribe.DailyAllowanceLeft = Constants.DailyAllowanceCap;
    }

    public static int GlobalAllowanceLeft()
    {
        // TODO: read accepted-quests-today counter; until then assume full allowance
        return Constants.DailyAllowanceCap;
    }
}
