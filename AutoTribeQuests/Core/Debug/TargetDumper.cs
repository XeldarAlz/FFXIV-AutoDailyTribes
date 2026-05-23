using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;

namespace AutoTribeQuests.Core.Debug;

// Debug command bound to /atq target. Logs the currently-targeted ENpc's
// BaseId + name and the player's current TerritoryType to chat. Use it to
// capture the real IssuerENpcBaseId for each tribe — stand next to the
// issuer, target it, /atq target, paste the result into TribeRegistry.cs.
internal static unsafe class TargetDumper
{
    public static void Dump()
    {
        var territoryId = Svc.ClientState.TerritoryType;
        var territoryName = Svc.Data.GetExcelSheet<TerritoryType>()
            ?.GetRowOrDefault(territoryId)
            ?.PlaceName.Value.Name.ToString() ?? "?";

        Svc.Chat.Print($"[ATQ] Territory: {territoryId} ({territoryName})");

        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Svc.Chat.Print("[ATQ] No target. Click an NPC first, then re-run /atq target.");
            return;
        }

        var baseId = target->BaseId;
        var name = target->NameString;
        var npcResident = Svc.Data.GetExcelSheet<ENpcResident>()
            ?.GetRowOrDefault(baseId);
        var residentName = npcResident?.Singular.ToString() ?? name;

        Svc.Chat.Print($"[ATQ] Target: BaseId={baseId}  Name=\"{residentName}\"  (instanceId=0x{target->GetGameObjectId():X})");
        Svc.Log.Info($"[TargetDumper] territory={territoryId} BaseId={baseId} name='{residentName}'");
    }
}
