using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Windows.Components;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class GeneralSettings
{
    private static readonly LocString[] OrderNames = [L.Settings.OrderNewest, L.Settings.OrderOldest];
    private static readonly LocString[] OrderDetails = [L.Settings.OrderNewestDetail, L.Settings.OrderOldestDetail];

    public static void Draw(Configuration cfg)
    {
        DrawLanguageGroup(cfg);
        DrawWindowGroup(cfg);
        DrawListGroup(cfg);
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

    private static void DrawListGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupList));

        var selected = cfg.ExpansionOrder == ExpansionOrder.OldestFirst ? 1 : 0;
        SettingsRow.Draw(Loc.T(L.Settings.ExpansionOrder), Loc.T(L.Settings.ExpansionOrderHelp), SettingsControls.RowComboWidth,
            () => SettingsControls.DrawChoices("##adt_order", OrderNames, OrderDetails, selected, choice =>
            {
                cfg.ExpansionOrder = choice == 1 ? ExpansionOrder.OldestFirst : ExpansionOrder.NewestFirst;
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(Loc.T(OrderDetails[selected]));
    }
}
