using ECommons.DalamudServices;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.Sheets;

namespace AutoDailyTribes.Core.Tribes;

internal static class IssuerResolver
{
    private const ulong LiveObjectIdMarker = 1ul << 32;

    public static void Resolve(TribeInfo tribe)
    {
        if (tribe.HasResolvedIssuers) return;

        if (Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(tribe.IssuerTerritoryId) is not { Bg.IsEmpty: false } territory)
        {
            RunLog.Warning($"[{tribe.Name}] No TerritoryType row for {tribe.IssuerTerritoryId}");
            return;
        }

        var scene = territory.Bg.ToString();
        var filenameStart = scene.LastIndexOf('/') + 1;
        var planeventLgb = "bg/" + scene[..filenameStart] + "planevent.lgb";

        var lgb = Svc.Data.GetFile<LgbFile>(planeventLgb);
        if (lgb is null)
        {
            RunLog.Warning($"[{tribe.Name}] failed to load {planeventLgb}");
            return;
        }

        var wanted = tribe.IssuerENpcBaseIds;
        var found = new TribeIssuer[wanted.Length];
        var foundCount = 0;
        foreach (var layer in lgb.Layers)
        {
            foreach (var instance in layer.InstanceObjects)
            {
                if (instance.AssetType != LayerEntryType.EventNPC) continue;
                var baseId = ((LayerCommon.ENPCInstanceObject)instance.Object).ParentData.ParentData.BaseId;
                var slot = Array.IndexOf(wanted, baseId);
                if (slot < 0 || found[slot].InstanceId != 0) continue;

                found[slot] = new TribeIssuer(baseId, LiveObjectIdMarker | instance.InstanceId, new(
                    instance.Transform.Translation.X,
                    instance.Transform.Translation.Y,
                    instance.Transform.Translation.Z));
                foundCount++;
            }
        }

        if (foundCount == 0)
        {
            RunLog.Warning($"[{tribe.Name}] none of the issuer ENpcs ({string.Join(", ", wanted)}) found in {planeventLgb}");
            return;
        }

        var issuers = new TribeIssuer[foundCount];
        var next = 0;
        for (var slot = 0; slot < found.Length; slot++)
        {
            if (found[slot].InstanceId == 0)
            {
                RunLog.Warning($"[{tribe.Name}] issuer ENpc {wanted[slot]} ({Name(wanted[slot])}) not found in {planeventLgb}");
                continue;
            }
            issuers[next++] = found[slot];
        }

        tribe.Issuers = issuers;
        RunLog.Info($"[{tribe.Name}] resolved {foundCount}/{wanted.Length} issuer(s): " +
                    string.Join(", ", Array.ConvertAll(issuers, issuer => $"{Name(issuer.BaseId)} at {issuer.Location} ({issuer.InstanceId:X})")));
    }

    public static string Name(uint issuerBaseId)
        => Svc.Data.GetExcelSheet<ENpcResident>()?.GetRowOrDefault(issuerBaseId)?.Singular.ToString() ?? issuerBaseId.ToString();
}
