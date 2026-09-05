using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class SettingsControls
{
    public const float ToggleWidth = 40f;
    public const float RowComboWidth = 190f;
    public const float ChoicePanelWidth = 340f;

    public static void DrawToggle(Configuration cfg, Func<bool> getter, Action<bool> setter, string id)
    {
        var value = getter();
        if (!ToggleSwitch.Draw(id, ref value)) return;

        setter(value);
        cfg.SaveDebounced();
    }

    public static void DrawChoices(string id, string[] names, string[] details, int selected, Action<int> onSelect, float width = RowComboWidth)
    {
        var picked = selected;
        if (Dropdown.DrawDetailed(id, names, details, ref picked, width, ChoicePanelWidth)) onSelect(picked);
    }

    public static IDisposable PushFrameColors()
        => ImRaii.PushColor(ImGuiCol.FrameBg, Styling.WithAlpha(Styling.Surface0, 0.9f))
            .Push(ImGuiCol.FrameBgHovered, Styling.Surface1)
            .Push(ImGuiCol.FrameBgActive, Styling.Surface1)
            .Push(ImGuiCol.Border, Styling.WithAlpha(Styling.BorderDim, 0.7f))
            .Push(ImGuiCol.Text, Styling.TextStrong);
}
