using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using AutoTribeQuests.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

// The main content area: era-grouped sections of tribe cards laid out as a
// responsive grid. Each card auto-fills its column; column count comes from
// Layout.TribeCardColumnsDefault and the available content width.
internal static class TribeList
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        foreach (var era in Enum.GetValues<TribeEra>())
        {
            var tribes = TribeRegistry.ByEra(era).ToArray();
            if (tribes.Length == 0) continue;

            Styling.SectionLabel(era.ToString());

            DrawGrid(tribes, controller, cfg);
            ImGui.Spacing();
        }
    }

    private static void DrawGrid(TribeInfo[] tribes, AutoTribeController controller, Configuration cfg)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var minCardWidth = Layout.TribeCardMinWidth * ImGuiHelpers.GlobalScale;
        var columns = Math.Max(1, Math.Min(tribes.Length, (int)(avail / minCardWidth)));

        using var table = ImRaii.Table($"##grid_{tribes[0].Era}", columns,
            ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoBordersInBody);
        if (!table) return;

        foreach (var tribe in tribes)
        {
            TribeStateReader.Refresh(tribe);
            ImGui.TableNextColumn();
            TribeCard.Draw(tribe, controller, cfg);
        }
    }
}
