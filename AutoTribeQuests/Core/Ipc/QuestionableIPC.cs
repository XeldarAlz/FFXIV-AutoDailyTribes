using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoTribeQuests.Core.Ipc;

// IPC strings verified against WigglyMuffin/Questionable @ Questionable/External/QuestionableIpc.cs.
internal sealed class QuestionableIPC
{
    private readonly ICallGateSubscriber<bool> _isRunning;
    private readonly ICallGateSubscriber<uint> _getCurrentQuestId;
    private readonly ICallGateSubscriber<uint, bool> _isQuestLocked;
    private readonly ICallGateSubscriber<uint, bool> _startQuest;
    private readonly ICallGateSubscriber<uint, bool> _startSingleQuest;
    private readonly ICallGateSubscriber<uint, bool> _addQuestPriority;
    private readonly ICallGateSubscriber<int, uint, bool> _insertQuestPriority;
    private readonly ICallGateSubscriber<bool> _clearQuestPriority;

    public QuestionableIPC()
    {
        _isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.IsRunning");
        _getCurrentQuestId = Svc.PluginInterface.GetIpcSubscriber<uint>("Questionable.GetCurrentQuestId");
        _isQuestLocked = Svc.PluginInterface.GetIpcSubscriber<uint, bool>("Questionable.IsQuestLocked");
        _startQuest = Svc.PluginInterface.GetIpcSubscriber<uint, bool>("Questionable.StartQuest");
        _startSingleQuest = Svc.PluginInterface.GetIpcSubscriber<uint, bool>("Questionable.StartSingleQuest");
        _addQuestPriority = Svc.PluginInterface.GetIpcSubscriber<uint, bool>("Questionable.AddQuestPriority");
        _insertQuestPriority = Svc.PluginInterface.GetIpcSubscriber<int, uint, bool>("Questionable.InsertQuestPriority");
        _clearQuestPriority = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.ClearQuestPriority");
    }

    public bool IsAvailable => _isRunning.HasFunction;

    public bool IsRunning() => _isRunning.HasFunction && _isRunning.InvokeFunc();
    public uint GetCurrentQuestId() => _getCurrentQuestId.HasFunction ? _getCurrentQuestId.InvokeFunc() : 0;
    public bool IsQuestLocked(uint questId) => !_isQuestLocked.HasFunction || _isQuestLocked.InvokeFunc(questId);
    public bool StartQuest(uint questId) => _startQuest.HasFunction && _startQuest.InvokeFunc(questId);
    public bool StartSingleQuest(uint questId) => _startSingleQuest.HasFunction && _startSingleQuest.InvokeFunc(questId);
    public bool AddPriority(uint questId) => _addQuestPriority.HasFunction && _addQuestPriority.InvokeFunc(questId);
    public bool InsertPriority(int index, uint questId) => _insertQuestPriority.HasFunction && _insertQuestPriority.InvokeFunc(index, questId);
    public bool ClearPriority() => _clearQuestPriority.HasFunction && _clearQuestPriority.InvokeFunc();
}
