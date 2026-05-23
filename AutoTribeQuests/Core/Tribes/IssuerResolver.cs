using ECommons.DalamudServices;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace AutoTribeQuests.Core.Tribes;

// Reads the territory's planevent.lgb file via Lumina, scans for the issuer's
// EventNPC instance, and writes back the live-world InstanceId + position.
//
// This is the same trick vsatisfy uses in CraftTurnin.cs to locate vendor + turn-in
// NPCs without hardcoding coordinates.
internal static class IssuerResolver
{
    public static void Resolve(TribeInfo tribe)
    {
        if (tribe.IssuerInstanceId != 0) return;

        if (Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(tribe.IssuerTerritoryId) is not { Bg.IsEmpty: false } territory)
        {
            Svc.Log.Warning($"[{tribe.Name}] No TerritoryType row for {tribe.IssuerTerritoryId}");
            return;
        }

        var scene = territory.Bg.ToString();
        var filenameStart = scene.LastIndexOf('/') + 1;
        var planeventLgb = "bg/" + scene[..filenameStart] + "planevent.lgb";

        var lgb = Svc.Data.GetFile<LgbFile>(planeventLgb);
        if (lgb is null)
        {
            Svc.Log.Warning($"[{tribe.Name}] failed to load {planeventLgb}");
            return;
        }

        foreach (var layer in lgb.Layers)
        {
            foreach (var instance in layer.InstanceObjects)
            {
                if (instance.AssetType != LayerEntryType.EventNPC) continue;
                var baseId = ((LayerCommon.ENPCInstanceObject)instance.Object).ParentData.ParentData.BaseId;
                if (baseId != tribe.IssuerENpcBaseId && !tribe.AltIssuerENpcBaseIds.Contains(baseId)) continue;

                tribe.IssuerInstanceId = (1ul << 32) | instance.InstanceId;
                tribe.IssuerLocation = new(
                    instance.Transform.Translation.X,
                    instance.Transform.Translation.Y,
                    instance.Transform.Translation.Z);
                Svc.Log.Info($"[{tribe.Name}] resolved issuer at {tribe.IssuerLocation} ({tribe.IssuerInstanceId:X})");
                return;
            }
        }

        Svc.Log.Warning($"[{tribe.Name}] issuer ENpc {tribe.IssuerENpcBaseId} not found in {planeventLgb}");
    }
}
