using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

internal static class ActionButton
{
    public static bool Draw(string label, bool enabled = true, float width = 0)
    {
        using var disabled = ImRaii.Disabled(!enabled);
        using var color = enabled
            ? Styling.PushAccentButtonColors()
            : ImRaii.PushColor(ImGuiCol.Button, Styling.CardBgSoft)
                .Push(ImGuiCol.ButtonHovered, Styling.CardBgSoft)
                .Push(ImGuiCol.ButtonActive, Styling.CardBgSoft);

        var size = new Vector2(width, Layout.ActionButtonHeight * ImGuiHelpers.GlobalScale);
        return ImGui.Button(label, size);
    }
}
