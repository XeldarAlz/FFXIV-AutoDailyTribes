using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class AddonInteractions
{
    public static bool InteractWith(ulong instanceId)
    {
        var obj = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(instanceId);
        if (obj == null) return false;
        TargetSystem.Instance()->InteractWithObject(obj, false);
        return true;
    }

    public static void ProgressTalk()
    {
        var a = AddonProbes.Get("Talk");
        if (a == null || !a->IsReady) return;
        var evt = new AtkEvent { Listener = &a->AtkEventListener, Target = &AtkStage.Instance()->AtkEventTarget };
        var data = new AtkEventData();
        a->ReceiveEvent(AtkEventType.MouseClick, 0, &evt, &data);
    }

    // Callback shapes verified against WigglyMuffin/Questionable's InteractionUiController:
    // SelectIconString / SelectString / SelectYesno / JournalResult all use
    // FireCallbackInt(N) — a single AtkValue containing the index (or 0/1 for yes/no).
    public static void SelectIconStringPick(int index)
    {
        var a = AddonProbes.Get("SelectIconString");
        if (a == null || !a->IsReady) return;
        a->FireCallbackInt(index);
    }

    public static void SelectIconStringCancel()
    {
        var a = AddonProbes.Get("SelectIconString");
        if (a == null || !a->IsReady) return;
        a->FireCallbackInt(-1);
    }

    public static void JournalAcceptConfirm()
    {
        var a = AddonProbes.Get("JournalAccept");
        if (a == null || !a->IsReady) return;
        a->FireCallbackInt(0);
    }
}
