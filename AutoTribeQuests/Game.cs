using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTribeQuests;

// Tribe-specific thin wrappers over FFXIVClientStructs. clib already covers
// generic Talk/SelectString/SelectYesno via its addon extensions, so this file
// is only the addons clib doesn't fully wrap yet (SelectIconString, JournalAccept).
public static unsafe class Game
{
    public static AtkUnitBase* GetAddonByName(string name)
        => RaptureAtkUnitManager.Instance()->GetAddonByName(name);

    public static bool IsAddonReady(string name)
    {
        var addon = GetAddonByName(name);
        return addon != null && addon->IsVisible && addon->IsReady;
    }

    public static bool IsTalkInProgress() => IsAddonReady("Talk");
    public static bool IsSelectStringActive() => IsAddonReady("SelectString");
    public static bool IsSelectIconStringActive() => IsAddonReady("SelectIconString");
    public static bool IsJournalAcceptActive() => IsAddonReady("JournalAccept");
    public static bool IsSelectYesnoActive() => IsAddonReady("SelectYesno");

    public static void ProgressTalk()
    {
        var addon = GetAddonByName("Talk");
        if (addon == null || !addon->IsReady) return;
        var evt = new AtkEvent { Listener = &addon->AtkEventListener, Target = &AtkStage.Instance()->AtkEventTarget };
        var data = new AtkEventData();
        addon->ReceiveEvent(AtkEventType.MouseClick, 0, &evt, &data);
    }

    // Fire-callback on SelectIconString. AtkValue[0] = "selection" event = 1, [1] = chosen index.
    // VERIFY at runtime: the callback shape on SelectIconString matches SelectString here, but
    // some addons use FireCallback(2, ...) — check Dalamud.Memory.ClickLib if behavior differs.
    public static void SelectIconString(int index)
    {
        var addon = GetAddonByName("SelectIconString");
        if (addon == null || !addon->IsReady) return;
        Span<AtkValue> values = stackalloc AtkValue[2];
        values[0].SetInt(1);
        values[1].SetInt(index);
        addon->FireCallback(2, (AtkValue*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref values[0]), true);
    }

    public static void CancelSelectIconString()
    {
        var addon = GetAddonByName("SelectIconString");
        if (addon == null || !addon->IsReady) return;
        AtkValue val = default;
        val.SetInt(-1);
        addon->FireCallback(1, &val, true);
    }

    // JournalAccept addon: pressing "Accept" is callback (0, true) by default. Verify in-game.
    public static void JournalAcceptConfirm()
    {
        var addon = GetAddonByName("JournalAccept");
        if (addon == null || !addon->IsReady) return;
        AtkValue val = default;
        val.SetInt(0);
        addon->FireCallback(1, &val, true);
    }

    // Read the quest IDs currently listed in the SelectIconString addon (the daily-pick menu).
    // SelectIconString stores option icons + text in atk node tables; this helper returns the
    // text-row count so the coroutine can iterate. Mapping text -> questId needs to either
    //   (a) match the Quest sheet name in the player's language, or
    //   (b) cross-reference the BeastTribeQuest sheet for THIS tribe + game's daily-cycle index
    // Option (b) is more robust. Implemented in the AutoTribe coroutine.
    public static int CountSelectIconStringOptions()
    {
        var addon = GetAddonByName("SelectIconString");
        if (addon == null) return 0;
        // SelectIconString's AtkValues[0] holds the option count (verify at runtime).
        if (addon->AtkValuesCount < 1) return 0;
        return addon->AtkValues[0].Int;
    }

    public static bool PlayerInRange(System.Numerics.Vector3 dest, float dist)
    {
        var p = Service.ClientState.LocalPlayer;
        if (p == null) return false;
        var d = dest - p.Position;
        return d.X * d.X + d.Z * d.Z <= dist * dist;
    }

    public static bool PlayerIsBusy()
        => Service.Conditions[ConditionFlag.BetweenAreas]
        || Service.Conditions[ConditionFlag.Casting]
        || ActionManager.Instance()->AnimationLock > 0;

    public static bool InteractWith(ulong instanceId)
    {
        var obj = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(instanceId);
        if (obj == null) return false;
        FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem.Instance()->InteractWithObject(obj, false);
        return true;
    }
}
