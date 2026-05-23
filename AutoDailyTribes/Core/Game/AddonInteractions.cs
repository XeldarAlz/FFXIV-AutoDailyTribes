using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoDailyTribes.Core.Game;

internal static unsafe class AddonInteractions
{
    // Mirror Questionable.GameFunctions.InteractWith: null the target first, set it, then call
    // InteractWithObject in the same pass. The null-then-set is what actually makes the game
    // recognise the new target — without it InteractWithObject quietly returns 7 (rejected).
    public static bool InteractWith(ulong instanceId)
    {
        var obj = Svc.Objects.SearchById(instanceId);
        if (obj == null) return false;

        Svc.Targets.Target = null;
        Svc.Targets.Target = obj;

        var result = TargetSystem.Instance()->InteractWithObject(obj.Struct(), false);
        return result != 7 && result > 0;
    }

    public static void ProgressTalk()
    {
        var a = AddonProbes.Get("Talk");
        if (a == null || !a->IsReady) return;
        var evt = new AtkEvent { Listener = &a->AtkEventListener, Target = &AtkStage.Instance()->AtkEventTarget };
        var data = new AtkEventData();
        a->ReceiveEvent(AtkEventType.MouseClick, 0, &evt, &data);
    }

    // Questionable's SelectIconStringPostSetup calls FireCallbackInt(index) — a single int value.
    // That's what Questionable.Controller.GameUi.InteractionUiController.SelectIconStringPostSetup does.
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

    // FireCallbackInt(0) on JournalAccept means "decline/close" — the quest never lands.
    // Accept is a button click on node 44; ECommons' AddonMaster wraps that correctly.
    public static void JournalAcceptConfirm()
    {
        var a = AddonProbes.Get("JournalAccept");
        if (a == null || !a->IsReady) return;
        new AddonMaster.JournalAccept((nint)a).Accept();
    }
}
