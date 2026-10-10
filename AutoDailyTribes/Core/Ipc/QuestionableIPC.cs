using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

internal sealed class QuestionableIPC
{
    private readonly ICallGateSubscriber<bool>         isRunning;
    private readonly ICallGateSubscriber<string, bool> startQuest;
    private readonly ICallGateSubscriber<string, bool> startSingleQuest;
    private readonly ICallGateSubscriber<string, bool> stop;
    private readonly ICallGateSubscriber<bool>         clearQuestPriority;
    private readonly ICallGateSubscriber<string, bool> addQuestPriority;
    private readonly ICallGateSubscriber<string, bool> isQuestAccepted;
    private readonly ICallGateSubscriber<string>       getCurrentQuestId;
    private readonly ICallGateSubscriber<string, bool> isReadyToAcceptQuest;
    private readonly ICallGateSubscriber<string, (bool, string)> isQuestLockedReason;

    public QuestionableIPC()
    {
        isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.IsRunning");
        startQuest         = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.StartQuest");
        startSingleQuest   = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.StartSingleQuest");
        stop               = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.Stop");
        clearQuestPriority = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.ClearQuestPriority");
        addQuestPriority   = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.AddQuestPriority");
        isQuestAccepted    = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.IsQuestAccepted");
        getCurrentQuestId  = Svc.PluginInterface.GetIpcSubscriber<string>("Questionable.GetCurrentQuestId");
        isReadyToAcceptQuest = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.IsReadyToAcceptQuest");
        isQuestLockedReason  = Svc.PluginInterface.GetIpcSubscriber<string, (bool, string)>("Questionable.IsQuestLockedReason");
    }

    public bool IsAvailable => isRunning.HasFunction;

    public bool IsRunning() => isRunning.HasFunction && isRunning.InvokeFunc();
    public bool StartQuest(uint questId) => startQuest.HasFunction && startQuest.InvokeFunc(Compact(questId));
    public bool StartSingleQuest(uint questId) => startSingleQuest.HasFunction && startSingleQuest.InvokeFunc(Compact(questId));
    public bool Stop(string label) => stop.HasFunction && stop.InvokeFunc(label);
    public bool ClearQuestPriority() => clearQuestPriority.HasFunction && clearQuestPriority.InvokeFunc();
    public bool AddQuestPriority(uint questId) => addQuestPriority.HasFunction && addQuestPriority.InvokeFunc(Compact(questId));
    public bool IsQuestAccepted(uint questId) => isQuestAccepted.HasFunction && isQuestAccepted.InvokeFunc(Compact(questId));

    // Compact decimal id of the quest Questionable is currently working on (null when idle/unavailable).
    public string? CurrentQuestId()
    {
        if (!getCurrentQuestId.HasFunction) return null;
        try { return getCurrentQuestId.InvokeFunc(); }
        catch (Exception ex) { RunLog.Warning(ex, "Questionable.GetCurrentQuestId failed"); return null; }
    }

    // A build without the probe cannot say no, so the quest is handed over and Questionable decides.
    public bool IsReadyToAcceptQuest(uint questId) => !isReadyToAcceptQuest.HasFunction || isReadyToAcceptQuest.InvokeFunc(Compact(questId));

    // Why Questionable will not accept the quest; empty when it is not locked or the build cannot say.
    public string LockedReason(uint questId)
    {
        if (!isQuestLockedReason.HasFunction) return string.Empty;
        try
        {
            var (locked, reason) = isQuestLockedReason.InvokeFunc(Compact(questId));
            return locked ? reason : string.Empty;
        }
        catch (Exception ex) { RunLog.Warning(ex, "Questionable.IsQuestLockedReason failed"); return string.Empty; }
    }

    public static string Compact(uint questId) => (questId & 0xFFFF).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
