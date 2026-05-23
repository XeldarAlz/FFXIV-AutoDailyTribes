using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;

namespace AutoDailyTribes.Core.Debug;

internal static unsafe class TargetDumper
{
    public static void Dump()
    {
        var territoryId = Svc.ClientState.TerritoryType;
        var territoryName = Svc.Data.GetExcelSheet<TerritoryType>()
            ?.GetRowOrDefault(territoryId)
            ?.PlaceName.Value.Name.ToString() ?? "?";

        Svc.Chat.Print($"[ADT] Territory: {territoryId} ({territoryName})");

        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Svc.Chat.Print("[ADT] No target. Click an NPC first, then re-run /adt target.");
            return;
        }

        var baseId = target->BaseId;
        var name = target->NameString;
        var residentName = Svc.Data.GetExcelSheet<ENpcResident>()
            ?.GetRowOrDefault(baseId)?.Singular.ToString() ?? name;

        Svc.Chat.Print($"[ADT] Target: BaseId={baseId}  Name=\"{residentName}\"");
        Svc.Log.Info($"[TargetDumper] territory={territoryId} BaseId={baseId} name='{residentName}'");
    }
}
