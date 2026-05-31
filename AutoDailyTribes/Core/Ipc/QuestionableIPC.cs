using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

// IPC strings + types verified against PunishXIV/Questionable's External/QuestionableIpc.cs.
// Quest IDs cross the wire as strings in compact 16-bit form (Questionable's ElementId.QuestId.ToString()).
internal sealed class QuestionableIPC
{
    private readonly ICallGateSubscriber<bool>         _isRunning;
    private readonly ICallGateSubscriber<string, bool> _startQuest;
    private readonly ICallGateSubscriber<string, bool> _stop;
    private readonly ICallGateSubscriber<bool>         _clearQuestPriority;
    private readonly ICallGateSubscriber<string, bool> _addQuestPriority;
    private readonly ICallGateSubscriber<string, bool> _isQuestAccepted;

    public QuestionableIPC()
    {
        _isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.IsRunning");
        // StartQuest puts Questionable into Automatic mode and runs the quest to completion (objectives +
        // turn-in), then rolls onto whatever it considers "current" next. We pin the tribe's accepted
        // dailies to the priority list first so it sticks to THOSE (priority-accepted quests outrank MSQ),
        // and call Stop the moment they're all turned in. StartSingleQuest is unusable here: it reverts to
        // Manual whenever the game's tracked quest != the one requested, so it dies after ~one step.
        _startQuest         = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.StartQuest");
        _stop               = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.Stop");
        _clearQuestPriority = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.ClearQuestPriority");
        _addQuestPriority   = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.AddQuestPriority");
        // IsQuestAccepted = currently in the journal. The right "is this daily still to do?" signal:
        // IsQuestComplete reads the permanent completed bit, which stays set for repeatable dailies.
        _isQuestAccepted    = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.IsQuestAccepted");
    }

    public bool IsAvailable => _isRunning.HasFunction;

    public bool IsRunning() => _isRunning.HasFunction && _isRunning.InvokeFunc();
    public bool StartQuest(uint questId) => _startQuest.HasFunction && _startQuest.InvokeFunc(ToCompact(questId));
    public bool Stop(string label) => _stop.HasFunction && _stop.InvokeFunc(label);
    public bool ClearQuestPriority() => _clearQuestPriority.HasFunction && _clearQuestPriority.InvokeFunc();
    public bool AddQuestPriority(uint questId) => _addQuestPriority.HasFunction && _addQuestPriority.InvokeFunc(ToCompact(questId));
    public bool IsQuestAccepted(uint questId) => _isQuestAccepted.HasFunction && _isQuestAccepted.InvokeFunc(ToCompact(questId));

    private static string ToCompact(uint questId) => (questId & 0xFFFF).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
