using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class GeneralSettings
{
    private static readonly TurnInMode[] TurnInOrder = [TurnInMode.EachQuest, TurnInMode.AllAtOnce];
    private static readonly LocString[] TurnInNames = [L.Settings.TurnInEach, L.Settings.TurnInAll];
    private static readonly LocString[] TurnInDetails = [L.Settings.TurnInEachDetail, L.Settings.TurnInAllDetail];

    public static void Draw(Configuration cfg)
    {
        DrawLanguageGroup(cfg);
        DrawWindowGroup(cfg);
        DrawDailiesGroup(cfg);
    }

    private static void DrawLanguageGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.Language));

        SettingsRow.Draw(Loc.T(L.Settings.Language), Loc.T(L.Settings.LanguageHelp), SettingsControls.RowComboWidth,
            () => SettingsControls.DrawLanguageCombo(cfg));
    }

    private static void DrawWindowGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupWindow));

        SettingsRow.Draw(Loc.T(L.Settings.OpenOnLogin), Loc.T(L.Settings.OpenOnLoginHelp), SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.AutoShowIfDailiesAvailable, value => cfg.AutoShowIfDailiesAvailable = value, "##adt_autoshow"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawDailiesGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupDailies));

        var selected = Math.Max(0, Array.IndexOf(TurnInOrder, cfg.TurnInMode));
        SettingsRow.Draw(Loc.T(L.Settings.TurnInMode), Loc.T(L.Settings.TurnInModeHelp), SettingsControls.RowComboWidth,
            () => SettingsControls.DrawChoices("##adt_turn_in", TurnInNames, TurnInDetails, selected, choice =>
            {
                cfg.TurnInMode = TurnInOrder[choice];
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(Loc.T(TurnInDetails[selected]));
    }
}
