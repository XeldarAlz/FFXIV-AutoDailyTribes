using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Components;

internal static class KindIcon
{
    public static void Draw(TribeKind kind)
    {
        var icon = kind switch
        {
            TribeKind.Combat   => FontAwesomeIcon.Shield,
            TribeKind.Crafter  => FontAwesomeIcon.Hammer,
            TribeKind.Gatherer => FontAwesomeIcon.Leaf,
            TribeKind.Mixed    => FontAwesomeIcon.Cubes,
        };
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.KindColor(kind)))
            ImGui.TextUnformatted(icon.ToIconString());
    }
}
