using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Components;

internal static class KindIcon
{
    public static FontAwesomeIcon Icon(TribeKind kind) => kind switch
    {
        TribeKind.Combat   => FontAwesomeIcon.Shield,
        TribeKind.Crafter  => FontAwesomeIcon.Hammer,
        TribeKind.Gatherer => FontAwesomeIcon.Leaf,
        TribeKind.Mixed    => FontAwesomeIcon.Cubes,
    };

    public static void Draw(TribeKind kind)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.KindColor(kind)))
            ImGui.TextUnformatted(Icon(kind).ToIconString());
    }
}
