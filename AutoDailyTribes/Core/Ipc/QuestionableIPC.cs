using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

// IPC strings + types verified against WigglyMuffin/Questionable's QuestionableIpc.cs.
// Quest IDs cross the wire as strings in compact 16-bit form (Questionable's ElementId.QuestId.ToString()).
internal sealed class QuestionableIPC
{
    private readonly ICallGateSubscriber<bool> _isRunning;
    private readonly ICallGateSubscriber<string, bool> _startSingleQuest;
    private readonly ICallGateSubscriber<string, bool> _isQuestComplete;

    public QuestionableIPC()
    {
        _isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.IsRunning");
        // StartSingleQuest runs exactly one quest then returns to Manual; StartQuest puts Questionable
        // into Automatic mode, which auto-advances onto the player's MSQ/side quests and never stops.
        _startSingleQuest = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.StartSingleQuest");
        _isQuestComplete = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.IsQuestComplete");
    }

    public bool IsAvailable => _isRunning.HasFunction;

    public bool IsRunning() => _isRunning.HasFunction && _isRunning.InvokeFunc();
    public bool StartSingleQuest(uint questId) => _startSingleQuest.HasFunction && _startSingleQuest.InvokeFunc(ToCompact(questId));
    public bool IsQuestComplete(uint questId) => _isQuestComplete.HasFunction && _isQuestComplete.InvokeFunc(ToCompact(questId));

    private static string ToCompact(uint questId) => (questId & 0xFFFF).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
