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

    private static readonly List<TribeInfo> cardBuffer = [];
    private static readonly List<TribeInfo> underRankBuffer = [];

    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        var eras = TribeRegistry.ErasNewestFirst;
        for (var eraIndex = 0; eraIndex < eras.Length; eraIndex++)
        {
            var (era, tribes) = eras[eraIndex];

            cardBuffer.Clear();
            underRankBuffer.Clear();
            var readyCount = 0;

            for (var i = 0; i < tribes.Length; i++)
            {
                if (IsRunnable(tribes[i])) { cardBuffer.Add(tribes[i]); readyCount++; }
            }
            for (var i = 0; i < tribes.Length; i++)
            {
                var tribe = tribes[i];
                if (tribe.Unlocked && tribe.MeetsRankRequirement && !IsRunnable(tribe)) cardBuffer.Add(tribe);
            }
            for (var i = 0; i < tribes.Length; i++)
            {
                if (!tribes[i].Unlocked) cardBuffer.Add(tribes[i]);
            }
            for (var i = 0; i < tribes.Length; i++)
            {
                var tribe = tribes[i];
                if (tribe.Unlocked && !tribe.MeetsRankRequirement) underRankBuffer.Add(tribe);
            }

            SectionHeader(era.DisplayName(), readyCount);
            Styling.VSpace(2);

            if (cardBuffer.Count > 0)
                DrawGrid($"##grid_{era}", cardBuffer, controller, cfg);

            if (underRankBuffer.Count > 0)
            {
                if (cardBuffer.Count > 0) Styling.VSpace(3);
                DrawChipFlow(underRankBuffer);
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
