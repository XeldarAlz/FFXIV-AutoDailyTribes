using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTribeQuests.Core.Game;

// All mutating interactions with the live game. Each method does exactly one
// thing — clicking a button, picking an option, advancing a dialog — and is
// safe to call when its addon isn't ready (silently no-ops).
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

    public static void SelectIconStringPick(int index)
    {
        var a = AddonProbes.Get("SelectIconString");
        if (a == null || !a->IsReady) return;
        Span<AtkValue> v = stackalloc AtkValue[2];
        v[0].SetInt(1);
        v[1].SetInt(index);
        fixed (AtkValue* p = v)
            a->FireCallback(2, p, true);
    }

    public static void SelectIconStringCancel()
    {
        var a = AddonProbes.Get("SelectIconString");
        if (a == null || !a->IsReady) return;
        AtkValue v = default;
        v.SetInt(-1);
        a->FireCallback(1, &v, true);
    }

    public static void JournalAcceptConfirm()
    {
        var a = AddonProbes.Get("JournalAccept");
        if (a == null || !a->IsReady) return;
        AtkValue v = default;
        v.SetInt(0);
        a->FireCallback(1, &v, true);
    }
}
