using AutoTribeQuests.Core.Tasks;
using AutoTribeQuests.Core.Tribes;
using AutoTribeQuests.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Sections;

internal static class TribeList
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        foreach (var era in Enum.GetValues<TribeEra>())
        {
            var tribes = TribeRegistry.ByEra(era).ToArray();
            if (tribes.Length == 0) continue;

            Styling.SectionLabel(era.DisplayName());

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
