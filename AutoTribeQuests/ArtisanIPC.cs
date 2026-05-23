using Dalamud.Plugin.Ipc;

namespace AutoTribeQuests;

// Optional dependency, used only for crafter tribes (Ixal/Moogles/Dwarves/etc.) if Questionable
// needs the plugin to drive the craft directly. Questionable already calls Artisan internally
// for crafting steps, so this wrapper is mostly here for diagnostics / fallback paths.
public sealed class ArtisanIPC
{
    private readonly ICallGateSubscriber<ushort, int, object> _craftItem;
    private readonly ICallGateSubscriber<bool> _enduranceStatus;

    public ArtisanIPC()
    {
        _craftItem = Service.PluginInterface.GetIpcSubscriber<ushort, int, object>("Artisan.CraftItem");
        _enduranceStatus = Service.PluginInterface.GetIpcSubscriber<bool>("Artisan.GetEnduranceStatus");
    }

    public bool IsAvailable => _enduranceStatus.HasFunction;

    public void CraftItem(ushort recipeId, int count) => _craftItem.InvokeAction(recipeId, count);
    public bool EnduranceRunning() => _enduranceStatus.HasFunction && _enduranceStatus.InvokeFunc();
}
