using AutoTribeQuests.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoTribeQuests.Windows.Components;

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
            _ => FontAwesomeIcon.Question,
        };
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.KindColor(kind)))
            ImGui.TextUnformatted(icon.ToIconString());
    }
}
