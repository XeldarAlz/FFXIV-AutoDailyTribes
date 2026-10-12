using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Ipc;
using clib.Enums;
using clib.Extensions;
using clib.Services;
using clib.TaskSystem;
using clib.Utils;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Network;
using System.Numerics;
using System.Threading.Tasks;
using GameSvc = ECommons.DalamudServices.Svc;

namespace AutoDailyTribes.Core.Tasks;

internal sealed partial class MoveOp
{
    private static NavmeshIPC Nav => NavmeshIPC.Instance;
    private static IPlayerCharacter? Player => GameSvc.Objects.LocalPlayer;

    private async Task Travel(uint territoryId, Vector3 destination, MovementConfig config,
        bool allowTeleportIfFaster, Func<bool>? stopCondition, bool allowAethernet)
    {
        await TeleportToTerritory(territoryId, destination, false);
        await UseAethernet(territoryId, destination);
        ErrorIf(GameSvc.ClientState.TerritoryType != territoryId, $"Failed to reach territory {territoryId}");
        await Navigate(destination, config, allowTeleportIfFaster, stopCondition, allowAethernet);
    }

    private async Task Navigate(Vector3 destination, MovementConfig config,
        bool allowTeleportIfFaster = false, Func<bool>? stopCondition = null, bool allowAethernet = false)
    {
        await WaitUntil(() => Player.Available, "WaitingForPlayer");
        var tolerance = Math.Max(config.Tolerance ?? 0, Nav.GetTolerance());
        if (Player.WithinRange(destination, tolerance) || stopCondition?.Invoke() == true) return;

        if (allowTeleportIfFaster && Coords.IsTeleportingFaster(destination))
            await TeleportToTerritory(GameSvc.ClientState.TerritoryType, destination, true);
        if (allowAethernet)
            await UseAethernet(GameSvc.ClientState.TerritoryType, destination);

        if (config.Movement.HasFlag(MovementOptions.Mount) || config.Movement.HasFlag(MovementOptions.Fly))
            await Mount();

        await WaitUntil(() => Nav.Ready || Nav.LoadingProgress >= 0, "WaitingForNavmesh");
        await WaitWhile(() => Nav.LoadingProgress >= 0, "BuildingNavmesh");
        ErrorIf(!Nav.Ready, "Could not load navmesh for this territory");
        await WaitWhile(() => Nav.IsPathfinding, "WaitingForPathfinding");
        CancelToken.ThrowIfCancellationRequested();

        // OnDispose also registers with AutoTask, so Cancel stops the path immediately.
        // It is idempotent: unwinding an old operation cannot stop a newer operation's path.
        using (new OnDispose(StopOwnedPath))
        {
            var fly = Player.InFlight || (config.Movement.HasFlag(MovementOptions.Fly) && Control.CanFly);
            ErrorIf(!Nav.PathfindAndMoveCloseTo(destination, fly, config.Tolerance ?? 3f),
                $"Could not start pathfinding to {destination}");
            Status = $"Moving to {destination}";
            await NextFrame();
            await WaitWhile(() => !Player.WithinRange(destination, tolerance)
                && stopCondition?.Invoke() != true
                && (Nav.IsMovePending || Nav.IsRunning()), "Navigate");
        }

        if (config.Movement.HasFlag(MovementOptions.Dismount) && Player.WithinRange(destination, tolerance))
            await Dismount();
    }

    private async Task TeleportToTerritory(uint territoryId, Vector3 destination, bool allowSameZoneTeleport)
    {
        // The Firmament is reached through Foundation's main aetheryte.
        var teleportTerritory = territoryId == 886 ? 418u : territoryId;
        var teleportDestination = territoryId == 886 ? Coords.AetherytePosition(70) : destination;
        if (GameSvc.ClientState.TerritoryType == territoryId && !allowSameZoneTeleport) return;

        await StopPathfinding();
        await WaitWhile(() => Player.IsUiFading, "WaitingForUi");
        var closest = Coords.FindClosestAetheryte(teleportTerritory, teleportDestination).GetValueOrDefault();
        var aetheryteId = Coords.FindPrimaryAetheryte(closest);
        ErrorIf(aetheryteId == 0, $"No unlocked aetheryte for territory {territoryId}");
        var aetheryte = GameSvc.Data.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>().GetRow(aetheryteId);

        if (GameSvc.ClientState.TerritoryType != aetheryte.Territory.RowId || allowSameZoneTeleport)
        {
            Status = $"Teleporting to {aetheryte.PlaceName.Value.Name}";
            var arrived = false;
            while (!arrived)
            {
                await StopPathfinding();
                await WaitWhile(() => Player.IsBusy, "WaitingForTeleport");
                CancelToken.ThrowIfCancellationRequested();
                ErrorIf(!ActionManager.Teleport(aetheryteId), $"Teleport to {aetheryteId} was rejected");
                var sawCast = false;
                var sawFade = false;
                while (true)
                {
                    var fading = Player.IsUiFading;
                    var casting = Player?.IsCasting == true;
                    sawCast |= casting;
                    sawFade |= fading;
                    if (sawFade && !fading)
                    {
                        await WaitUntil(() => GameMain.IsTerritoryLoaded && Player.Interactable, "TeleportArrival");
                        arrived = true;
                        break;
                    }
                    if (sawCast && !casting && !sawFade && ActionManager.GetCastAction().Elapsed != 5f)
                        break;
                    await NextFrame();
                }
            }
        }

        if (GameSvc.ClientState.TerritoryType != territoryId)
            await UseAethernet(territoryId, destination);
        ErrorIf(GameSvc.ClientState.TerritoryType != territoryId, $"Failed to reach territory {territoryId}");
    }

    private async Task UseAethernet(uint territoryId, Vector3 destination)
    {
        if (territoryId == 886)
        {
            if (GameSvc.ClientState.TerritoryType == 886) return;
            await ApproachAetheryte(70);
            await WaitForTravelMenu("SelectString");
            PacketDispatcher.TeleportToFirmament(70);
            await WaitUntil(() => GameSvc.ClientState.TerritoryType == 886
                && GameMain.IsTerritoryLoaded && Player.Interactable, "EnterFirmament");
            return;
        }

        var source = Coords.FindClosestAetheryte(GameSvc.ClientState.TerritoryType, Player!.Position).GetValueOrDefault();
        var target = Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault();
        if (source == 0 || target == 0 || source == target) return;
        var primary = Coords.FindPrimaryAetheryte(source);
        if (primary == 0 || primary != Coords.FindPrimaryAetheryte(target)) return;

        if (Coords.FindAetheryte(source).Item1 == 0) source = primary;
        await ApproachAetheryte(source);
        var isMain = GameSvc.Data.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>().GetRow(source).IsAetheryte;
        await WaitForTravelMenu(isMain ? "SelectString" : "TelepotTown");
        PacketDispatcher.TeleportToAethernet(source, target);
        await WaitUntilThenFalse(() => GameSvc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas], "AethernetTransition");
        await WaitUntil(() => GameSvc.ClientState.TerritoryType == territoryId
            && GameMain.IsTerritoryLoaded && Player.Interactable, "AethernetArrival");
    }

    private async Task ApproachAetheryte(uint aetheryteId)
    {
        var (objectId, position) = Coords.FindAetheryte(aetheryteId);
        ErrorIf(objectId == 0, $"Aetheryte {aetheryteId} is not available nearby");
        if (!Player.WithinRange(position, InteractRange.Aetheryte.MaxDistance))
            await Navigate(position, MovementConfig.Default.WithTolerance(InteractRange.Aetheryte));
        if (Player.Mounted) await Dismount();
        CancelToken.ThrowIfCancellationRequested();
        ErrorIf(!AddonInteractions.InteractWith(objectId), $"Could not interact with aetheryte {aetheryteId}");
    }

    private async Task WaitForTravelMenu(string addon)
    {
        while (!TravelMenuReady(addon))
        {
            CancelToken.ThrowIfCancellationRequested();
            AddonInteractions.ProgressTalk();
            await NextFrame();
        }
    }

    private static unsafe bool TravelMenuReady(string addon)
        => FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase.IsAddonReady(addon);

    private async Task Mount()
    {
        if (!Player.CanMount) return;
        Status = "Mounting";
        while (!Player.Mounted)
        {
            CancelToken.ThrowIfCancellationRequested();
            if (!Player.IsBusy && !ActionManager.IsActionInUse(ActionType.GeneralAction, 24))
                ActionManager.UseAction(ActionType.GeneralAction, 24);
            await NextFrame();
        }
    }

    private async Task Dismount()
    {
        if (Player is null || !Player.Mounted) return;
        if (Player.InFlight && Nav.NearestPointReachable(Player.Position) is { } ground)
            await Navigate(ground, MovementConfig.Everything);
        Status = "Dismounting";
        while (Player.Mounted)
        {
            CancelToken.ThrowIfCancellationRequested();
            if (Player.InFlight && !Player.IsAirDismountable)
                ActionManager.UseAction(ActionType.GeneralAction, 23);
            else if (Player.InFlight)
                GameMain.ExecuteLocationCommand(LocationCommandFlag.Dismount, Player!.Position, (int)Player.PackedRotation);
            else
                GameMain.ExecuteCommand(CommandFlag.Dismount, 1);
            await NextFrame();
        }
    }

    private async Task StopPathfinding()
    {
        CancelToken.ThrowIfCancellationRequested();
        Nav.Stop();
        if (!Nav.IsMovePending) return;
        await WaitWhile(() => Nav.IsMovePending, "StopPathfinding");
        Nav.Stop();
    }

    private static void StopOwnedPath()
    {
        try { Nav.Stop(); }
        catch (Exception ex) { RunLog.Warning(ex, "Could not stop the movement path during cleanup"); }
    }
}
