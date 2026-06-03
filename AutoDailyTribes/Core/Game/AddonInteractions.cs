using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class AddonInteractions
{
    public static bool InteractWith(ulong instanceId)
    {
        var obj = Svc.Objects.SearchById(instanceId);
        if (obj == null) return false;

        Svc.Targets.Target = null;
        Svc.Targets.Target = obj;

        var result = TargetSystem.Instance()->InteractWithObject(obj.Struct(), false);
        return result != 7 && result > 0;  // 7 = rejected
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
        new AddonMaster.JournalAccept((nint)a).Accept();
    }
}
