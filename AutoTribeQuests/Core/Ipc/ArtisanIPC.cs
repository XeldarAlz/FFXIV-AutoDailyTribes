using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoTribeQuests.Core.Ipc;

internal sealed class ArtisanIPC
{
    private readonly ICallGateSubscriber<ushort, int, object> _craftItem;
    private readonly ICallGateSubscriber<bool> _enduranceStatus;

    public ArtisanIPC()
    {
        _craftItem = Svc.PluginInterface.GetIpcSubscriber<ushort, int, object>("Artisan.CraftItem");
        _enduranceStatus = Svc.PluginInterface.GetIpcSubscriber<bool>("Artisan.GetEnduranceStatus");
    }

    public bool IsAvailable => _enduranceStatus.HasFunction;

    public void CraftItem(ushort recipeId, int count) => _craftItem.InvokeAction(recipeId, count);
    public bool EnduranceRunning() => _enduranceStatus.HasFunction && _enduranceStatus.InvokeFunc();
}
