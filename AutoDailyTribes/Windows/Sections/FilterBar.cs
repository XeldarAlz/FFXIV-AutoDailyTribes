using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class FilterBar
{
    private readonly record struct KindChip(TribeKind Kind, string Label, string Id, string HideHint, string ShowHint);

    private static readonly KindChip[] KindChips = BuildKindChips();

    private static float rowRightEdge;
    private static float rowCursorX;
    private static bool rowStarted;

    public static bool PassesKindFilter(Configuration cfg, TribeInfo tribe)
        => !cfg.HiddenKinds.Contains(tribe.Kind);

    public static void Draw(Configuration cfg)
    {
        rowRightEdge = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        rowStarted = false;

        if (Place("Ready only", "readyonly", cfg.ShowReadyOnly, Styling.AccentTeal))
        {
            cfg.ShowReadyOnly = !cfg.ShowReadyOnly;
            cfg.SaveDebounced();
        }
        Tooltip.For(cfg.ShowReadyOnly
            ? "Showing only tribes with dailies left today. Click to bring finished, locked and under-rank tribes back."
            : "Hide everything you can't run right now — finished, locked and under-rank tribes.");

        for (var chipIndex = 0; chipIndex < KindChips.Length; chipIndex++)
        {
            var chip = KindChips[chipIndex];
            if (!IsKindInPlay(cfg, chip.Kind)) continue;

            var shown = !cfg.HiddenKinds.Contains(chip.Kind);
            if (Place(chip.Label, chip.Id, shown, Styling.KindColor(chip.Kind)))
            {
                if (shown) cfg.HiddenKinds.Add(chip.Kind);
                else cfg.HiddenKinds.Remove(chip.Kind);
                cfg.SaveDebounced();
            }
            Tooltip.For(shown ? chip.HideHint : chip.ShowHint);
        }

        if (!cfg.ShowReadyOnly && cfg.HiddenKinds.Count == 0) return;

        var hidden = CountHidden(cfg);
        if (Place(hidden > 0 ? $"Reset · {hidden} hidden" : "Reset", "resetfilters", false, Styling.AccentAmber))
        {
            cfg.ShowReadyOnly = false;
            cfg.HiddenKinds.Clear();
            cfg.SaveDebounced();
        }
        Tooltip.For(hidden > 0
            ? $"{hidden} unlocked tribe(s) hidden by the filters above — click to show everything again."
            : "Clear the filters above.");
    }

    private static bool IsKindInPlay(Configuration cfg, TribeKind kind)
    {
        if (cfg.HiddenKinds.Contains(kind)) return true;

        var tribes = TribeRegistry.Tribes;
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            if (tribes[tribeIndex].Kind == kind && tribes[tribeIndex].Unlocked) return true;
        }
        return false;
    }

    private static bool Place(string label, string id, bool active, Vector4 accent)
    {
        var spacing = 6f * ImGuiHelpers.GlobalScale;
        if (rowStarted && rowCursorX + spacing + Chip.Width(label) < rowRightEdge) ImGui.SameLine(0, spacing);
        rowStarted = true;

        var clicked = Chip.Draw(label, id, active, accent);
        rowCursorX = ImGui.GetItemRectMax().X;
        return clicked;
    }

    private static int CountHidden(Configuration cfg)
    {
        var tribes = TribeRegistry.Tribes;
        var hidden = 0;
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++)
        {
            var tribe = tribes[tribeIndex];
            if (!tribe.Unlocked) continue;
            if (!PassesKindFilter(cfg, tribe)) hidden++;
            else if (cfg.ShowReadyOnly && !TribeList.IsRunnable(tribe)) hidden++;
        }
        return hidden;
    }

    private static KindChip[] BuildKindChips()
    {
        var kinds = Enum.GetValues<TribeKind>();
        var chips = new KindChip[kinds.Length];
        for (var kindIndex = 0; kindIndex < chips.Length; kindIndex++)
        {
            var kind = kinds[kindIndex];
            var label = kind.ToString();
            chips[kindIndex] = new KindChip(kind, label, $"kind_{label}", $"Hide {label} tribes", $"Show {label} tribes");
        }
        return chips;
    }
}
