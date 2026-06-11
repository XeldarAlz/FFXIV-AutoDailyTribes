using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;

namespace AutoDailyTribes.Windows.Sections;

internal static class SetupPanel
{
    public static void Draw(AutoTribeController controller, Configuration cfg)
    {
        StatusPanel.Draw();
        ImGui.Spacing();
        Header.Draw(controller, cfg);
        TribeList.Draw(controller, cfg);
    }
}
