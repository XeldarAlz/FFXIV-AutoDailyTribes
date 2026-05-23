using clib.TaskSystem;
using System.Threading.Tasks;

namespace AutoTribeQuests;

public abstract class AutoCommon : TaskBase
{
    // Wait until condition is met, auto-progressing Talk dialogs as they appear.
    protected async Task WaitUntilSkipTalk(Func<bool> condition, string scopeName)
    {
        using var scope = BeginScope(scopeName);
        while (!condition())
        {
            if (Game.IsTalkInProgress())
            {
                Log("progressing talk...");
                Game.ProgressTalk();
            }
            await NextFrame();
        }
    }

    protected static string QuestName(uint questId) =>
        Service.LuminaRow<Lumina.Excel.Sheets.Quest>(questId)?.Name.ToString() ?? questId.ToString();
}
