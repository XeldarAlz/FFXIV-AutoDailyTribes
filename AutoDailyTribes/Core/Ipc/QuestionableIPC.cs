using Dalamud.Plugin.Ipc;
using ECommons.Automation;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

// WigglyQuest is a Questionable fork that answers the same calls under its own prefix. It has no Stop
// or IsQuestLockedReason, so it is stopped through its chat command. It is the one asked when both are loaded.
internal sealed class QuestionableIPC
{
    private readonly QuestingPlugin wigglyQuest = new("WigglyQuest", "/wqst stop");
    private readonly QuestingPlugin questionable = new("Questionable", "/qst stop");

    private QuestingPlugin Active => wigglyQuest.IsRunning.HasFunction ? wigglyQuest : questionable;

    public bool IsAvailable => Active.IsRunning.HasFunction;

    public bool IsRunning()
    {
        var gate = Active.IsRunning;
        return gate.HasFunction && gate.InvokeFunc();
    }

    public bool StartQuest(uint questId) => Invoke(Active.StartQuest, questId);
    public bool StartSingleQuest(uint questId) => Invoke(Active.StartSingleQuest, questId);
    public bool AddQuestPriority(uint questId) => Invoke(Active.AddQuestPriority, questId);
    public bool IsQuestAccepted(uint questId) => Invoke(Active.IsQuestAccepted, questId);

    public bool ClearQuestPriority()
    {
        var gate = Active.ClearQuestPriority;
        return gate.HasFunction && gate.InvokeFunc();
    }

    public bool Stop(string label)
    {
        var active = Active;
        if (active.Stop.HasFunction) return active.Stop.InvokeFunc(label);
        if (!active.IsRunning.HasFunction) return false;

        Chat.ExecuteCommand(active.StopCommand);
        return true;
    }

    // Compact decimal id of the quest Questionable is currently working on (null when idle/unavailable).
    public string? CurrentQuestId()
    {
        var gate = Active.GetCurrentQuestId;
        if (!gate.HasFunction) return null;
        try { return gate.InvokeFunc(); }
        catch (Exception ex) { RunLog.Warning(ex, "Questionable.GetCurrentQuestId failed"); return null; }
    }

    // A build without the probe cannot say no, so the quest is handed over and Questionable decides.
    public bool IsReadyToAcceptQuest(uint questId)
    {
        var gate = Active.IsReadyToAcceptQuest;
        return !gate.HasFunction || gate.InvokeFunc(Compact(questId));
    }

    // Why Questionable will not accept the quest; empty when it is not locked or the build cannot say.
    public string LockedReason(uint questId)
    {
        var gate = Active.IsQuestLockedReason;
        if (!gate.HasFunction) return string.Empty;
        try
        {
            var (locked, reason) = gate.InvokeFunc(Compact(questId));
            return locked ? reason : string.Empty;
        }
        catch (Exception ex) { RunLog.Warning(ex, "Questionable.IsQuestLockedReason failed"); return string.Empty; }
    }

    public static string Compact(uint questId) => (questId & 0xFFFF).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static bool Invoke(ICallGateSubscriber<string, bool> gate, uint questId)
        => gate.HasFunction && gate.InvokeFunc(Compact(questId));

    private sealed class QuestingPlugin
    {
        public QuestingPlugin(string ipcPrefix, string stopCommand)
        {
            var pluginInterface = Svc.PluginInterface;
            IsRunning            = pluginInterface.GetIpcSubscriber<bool>(ipcPrefix + ".IsRunning");
            StartQuest           = pluginInterface.GetIpcSubscriber<string, bool>(ipcPrefix + ".StartQuest");
            StartSingleQuest     = pluginInterface.GetIpcSubscriber<string, bool>(ipcPrefix + ".StartSingleQuest");
            Stop                 = pluginInterface.GetIpcSubscriber<string, bool>(ipcPrefix + ".Stop");
            ClearQuestPriority   = pluginInterface.GetIpcSubscriber<bool>(ipcPrefix + ".ClearQuestPriority");
            AddQuestPriority     = pluginInterface.GetIpcSubscriber<string, bool>(ipcPrefix + ".AddQuestPriority");
            IsQuestAccepted      = pluginInterface.GetIpcSubscriber<string, bool>(ipcPrefix + ".IsQuestAccepted");
            GetCurrentQuestId    = pluginInterface.GetIpcSubscriber<string>(ipcPrefix + ".GetCurrentQuestId");
            IsReadyToAcceptQuest = pluginInterface.GetIpcSubscriber<string, bool>(ipcPrefix + ".IsReadyToAcceptQuest");
            IsQuestLockedReason  = pluginInterface.GetIpcSubscriber<string, (bool, string)>(ipcPrefix + ".IsQuestLockedReason");
            StopCommand          = stopCommand;
        }

        public ICallGateSubscriber<bool>                    IsRunning { get; }
        public ICallGateSubscriber<string, bool>            StartQuest { get; }
        public ICallGateSubscriber<string, bool>            StartSingleQuest { get; }
        public ICallGateSubscriber<string, bool>            Stop { get; }
        public ICallGateSubscriber<bool>                    ClearQuestPriority { get; }
        public ICallGateSubscriber<string, bool>            AddQuestPriority { get; }
        public ICallGateSubscriber<string, bool>            IsQuestAccepted { get; }
        public ICallGateSubscriber<string>                  GetCurrentQuestId { get; }
        public ICallGateSubscriber<string, bool>            IsReadyToAcceptQuest { get; }
        public ICallGateSubscriber<string, (bool, string)>  IsQuestLockedReason { get; }
        public string                                       StopCommand { get; }
    }
}
