using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class SettingsControls
{
    public const float ToggleWidth = 40f;
    public const float RowComboWidth = 190f;
    public const float ChoicePanelWidth = 340f;

    private static readonly string[] languageLabels = BuildLanguageLabels();
    private static string[] choiceNames = [];
    private static string[] choiceDetails = [];

    public static void DrawToggle(Configuration cfg, Func<bool> getter, Action<bool> setter, string id)
    {
        var value = getter();
        if (!ToggleSwitch.Draw(id, ref value)) return;

        setter(value);
        cfg.SaveDebounced();
    }

    // The catalog hands back cached strings, so refilling shared buffers keeps translating a choice
    // list allocation free per frame.
    public static void DrawChoices(string id, ReadOnlySpan<LocString> names, ReadOnlySpan<LocString> details,
        int selected, Action<int> onSelect, float width = RowComboWidth)
    {
        if (choiceNames.Length < names.Length)
        {
            choiceNames = new string[names.Length];
            choiceDetails = new string[names.Length];
        }

        for (var index = 0; index < names.Length; index++)
        {
            choiceNames[index] = Loc.T(names[index]);
            choiceDetails[index] = Loc.T(details[index]);
        }

        var picked = selected;
        if (Dropdown.DrawDetailed(id, choiceNames.AsSpan(0, names.Length), choiceDetails.AsSpan(0, names.Length),
                ref picked, width, ChoicePanelWidth))
        {
            onSelect(picked);
        }
    }

    public static void DrawLanguageCombo(Configuration cfg, float width = RowComboWidth)
    {
        var languages = Languages.All;
        var selected = 0;
        for (var index = 0; index < languages.Length; index++)
        {
            if (ReferenceEquals(languages[index], Loc.Current)) selected = index;
        }

        if (!Dropdown.Draw("##adt_language", languageLabels, ref selected, width)) return;

        cfg.Language = languages[selected].Code;
        cfg.Save();
        Loc.SetLanguage(cfg.Language);
        Fonts.OnLanguageChanged();
        Plugin.Instance.OnLanguageChanged();
    }

    public static IDisposable PushFrameColors()
        => ImRaii.PushColor(ImGuiCol.FrameBg, Styling.WithAlpha(Styling.Surface0, 0.9f))
            .Push(ImGuiCol.FrameBgHovered, Styling.Surface1)
            .Push(ImGuiCol.FrameBgActive, Styling.Surface1)
            .Push(ImGuiCol.Border, Styling.WithAlpha(Styling.BorderDim, 0.7f))
            .Push(ImGuiCol.Text, Styling.TextStrong);

    private static string[] BuildLanguageLabels()
    {
        var languages = Languages.All;
        var labels = new string[languages.Length];
        for (var index = 0; index < labels.Length; index++) labels[index] = languages[index].NativeName;
        return labels;
    }
}
