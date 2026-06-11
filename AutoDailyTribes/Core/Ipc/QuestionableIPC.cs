using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoDailyTribes.Core.Ipc;

internal sealed class QuestionableIPC
{
    private readonly ICallGateSubscriber<bool>         isRunning;
    private readonly ICallGateSubscriber<string, bool> startQuest;
    private readonly ICallGateSubscriber<string, bool> stop;
    private readonly ICallGateSubscriber<bool>         clearQuestPriority;
    private readonly ICallGateSubscriber<string, bool> addQuestPriority;
    private readonly ICallGateSubscriber<string, bool> isQuestAccepted;
    private readonly ICallGateSubscriber<string>       getCurrentQuestId;

    public QuestionableIPC()
    {
        isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.IsRunning");
        startQuest         = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.StartQuest");
        stop               = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.Stop");
        clearQuestPriority = Svc.PluginInterface.GetIpcSubscriber<bool>("Questionable.ClearQuestPriority");
        addQuestPriority   = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.AddQuestPriority");
        isQuestAccepted    = Svc.PluginInterface.GetIpcSubscriber<string, bool>("Questionable.IsQuestAccepted");
        getCurrentQuestId  = Svc.PluginInterface.GetIpcSubscriber<string>("Questionable.GetCurrentQuestId");
    }

    public bool IsAvailable => isRunning.HasFunction;

    public bool IsRunning() => isRunning.HasFunction && isRunning.InvokeFunc();
    public bool StartQuest(uint questId) => startQuest.HasFunction && startQuest.InvokeFunc(Compact(questId));
    public bool Stop(string label) => stop.HasFunction && stop.InvokeFunc(label);
    public bool ClearQuestPriority() => clearQuestPriority.HasFunction && clearQuestPriority.InvokeFunc();
    public bool AddQuestPriority(uint questId) => addQuestPriority.HasFunction && addQuestPriority.InvokeFunc(Compact(questId));
    public bool IsQuestAccepted(uint questId) => isQuestAccepted.HasFunction && isQuestAccepted.InvokeFunc(Compact(questId));

    // Compact decimal id of the quest Questionable is currently working on (null when idle/unavailable).
    public string? CurrentQuestId()
    {
        if (!getCurrentQuestId.HasFunction) return null;
        try { return getCurrentQuestId.InvokeFunc(); }
        catch (Exception ex) { Svc.Log.Warning(ex, "[ADT] Questionable.GetCurrentQuestId failed"); return null; }
    }

    public static string Compact(uint questId) => (questId & 0xFFFF).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
