using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Sections;

internal static class TribeList
{
    public static bool IsRunnable(TribeInfo t)
        => t.Unlocked && t.MeetsRankRequirement && (t.AcceptSlotsRemaining > 0 || t.HasInProgressQuests);

    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        // Reverse() = newest expansion first (enum runs ARR..DT).
        foreach (var era in Enum.GetValues<TribeEra>().Reverse())
        {
            var tribes = TribeRegistry.ByEra(era).ToArray();
            if (tribes.Length == 0) continue;

            var ready = tribes.Where(IsRunnable).ToList();
            var done = tribes.Where(t => t.Unlocked && t.MeetsRankRequirement && !IsRunnable(t)).ToList();
            var locked = tribes.Where(t => !t.Unlocked).ToList();
            var underRank = tribes.Where(t => t.Unlocked && !t.MeetsRankRequirement).ToList();

            SectionHeader(era.DisplayName(), ready.Count);
            Styling.VSpace(2);

            // Ready, done and locked share one grid so non-ready cards keep the same column width
            // and sit in their natural cell — dimmed, not blown up into a separate full-width row.
            var cards = ready.Concat(done).Concat(locked).ToList();
            if (cards.Count > 0)
                DrawGrid($"##grid_{era}", cards, controller, cfg);

            if (underRank.Count > 0)
            {
                if (cards.Count > 0) Styling.VSpace(3);
                DrawChipFlow(underRank);
            }

            Styling.VSpace(9);
        }
    }

    private static void SectionHeader(string label, int readyCount)
    {
        var s = ImGuiHelpers.GlobalScale;
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(label.ToUpperInvariant());

        if (readyCount > 0)
        {
            ImGui.SameLine(0, 7f * s);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentTeal))
                ImGui.TextUnformatted($"{readyCount} ready");
        }

        var lineY = ImGui.GetItemRectMin().Y + ImGui.GetItemRectSize().Y * 0.5f;
        var leftX = ImGui.GetItemRectMax().X + 8f * s;
        var rightX = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        if (rightX > leftX)
            ImGui.GetWindowDrawList().AddLine(new Vector2(leftX, lineY), new Vector2(rightX, lineY),
                ImGui.GetColorU32(Styling.Hairline), 1f);
    }

    private static void DrawGrid(string id, List<TribeInfo> tribes, AutoTribeController controller, Configuration cfg)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var minCardWidth = Layout.TribeCardMinWidth * ImGuiHelpers.GlobalScale;
        var columns = Math.Max(1, Math.Min(tribes.Count, (int)(avail / minCardWidth)));

        using var table = ImRaii.Table(id, columns, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoBordersInBody);
        if (!table) return;

        foreach (var tribe in tribes)
        {
            ImGui.TableNextColumn();
            if (!tribe.Unlocked) TribeCard.DrawLocked(tribe);
            else if (IsRunnable(tribe)) TribeCard.Draw(tribe, controller, cfg);
            else TribeCard.DrawDone(tribe);
        }
    }

    private static void DrawChipFlow(List<TribeInfo> tribes)
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var rightEdge = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        for (var i = 0; i < tribes.Count; i++)
        {
            if (i > 0 && ImGui.GetItemRectMax().X + spacing + TribeChip.Width(tribes[i]) < rightEdge)
                ImGui.SameLine();
            TribeChip.Draw(tribes[i]);
        }
    }
}
