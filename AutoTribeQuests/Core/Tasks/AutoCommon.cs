using AutoTribeQuests.Core.Game;
using clib.TaskSystem;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoTribeQuests.Core.Tasks;

// Shared helpers for tribe-automation tasks. Subclasses Dalamud's TaskBase
// (from clib) so they pick up MoveTo / TeleportTo / InteractWith and the
// cooperative-cancel framework.
public abstract class AutoCommon : TaskBase
{
    protected async Task WaitUntilSkipTalk(Func<bool> condition, string scopeName)
    {
        using var scope = BeginScope(scopeName);
        while (!condition())
        {
            if (AddonProbes.TalkActive())
            {
                Log("progressing talk...");
                AddonInteractions.ProgressTalk();
            }
            await NextFrame();
        }
    }

    protected static string QuestName(uint questId)
        => Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Quest>()?.GetRowOrDefault(questId)?.Name.ToString() ?? questId.ToString();
}
