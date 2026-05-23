using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoTribeQuests.Windows.Components;

internal static class ActionButton
{
    public static bool Draw(string label, bool enabled = true, float width = 0)
    {
        using var disabled = ImRaii.Disabled(!enabled);
        using var color = enabled
            ? ImRaii.PushColor(ImGuiCol.Button, Styling.AccentTeal * 0.55f)
                .Push(ImGuiCol.ButtonHovered, Styling.AccentTeal * 0.75f)
                .Push(ImGuiCol.ButtonActive, Styling.AccentTeal)
            : ImRaii.PushColor(ImGuiCol.Button, Styling.CardBgSoft)
                .Push(ImGuiCol.ButtonHovered, Styling.CardBgSoft)
                .Push(ImGuiCol.ButtonActive, Styling.CardBgSoft);

        var size = new Vector2(width, Layout.ActionButtonHeight * ImGuiHelpers.GlobalScale);
        return ImGui.Button(label, size);
    }
}
