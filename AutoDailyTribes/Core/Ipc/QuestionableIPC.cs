using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

// IPC strings + types verified against WigglyMuffin/Questionable's QuestionableIpc.cs.
// Quest IDs cross the wire as strings in compact 16-bit form (Questionable's ElementId.QuestId.ToString()).
internal sealed class QuestionableIPC
{
    private readonly ICallGateSubscriber<bool> _isRunning;
    private readonly ICallGateSubscriber<string, bool> _startQuest;

    public QuestionableIPC()
    {
        _isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.IsRunning");
        _startQuest = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.StartQuest");
    }

    public bool IsAvailable => _isRunning.HasFunction;

    public bool IsRunning() => _isRunning.HasFunction && _isRunning.InvokeFunc();
    public bool StartQuest(uint questId) => _startQuest.HasFunction && _startQuest.InvokeFunc(ToCompact(questId));

    private static string ToCompact(uint questId) => (questId & 0xFFFF).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
