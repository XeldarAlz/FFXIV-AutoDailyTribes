using AutoDailyTribes.Windows.Components;

namespace AutoDailyTribes.Windows.Sections.Config;

internal static class GeneralSettings
{
    private const string WindowGroup = "Window";
    private const string OpenOnLogin = "Open on login when dailies are available";
    private const string OpenOnLoginHelp = "Shows this window after logging in whenever you still have daily allowances to spend.";
    private const string ListGroup = "Tribe list";
    private const string OrderLabel = "Expansion order";
    private const string OrderHelp = "Which end of the expansion picker the newest content sits on.";

    private static readonly string[] OrderNames = ["Newest first", "Oldest first"];
    private static readonly string[] OrderDetails =
    [
        "Dawntrail leads the expansion picker, A Realm Reborn closes it.",
        "A Realm Reborn leads the expansion picker, Dawntrail closes it.",
    ];

    public static void Draw(Configuration cfg)
    {
        DrawWindowGroup(cfg);
        DrawListGroup(cfg);
    }

    private static void DrawWindowGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(WindowGroup);

        SettingsRow.Draw(OpenOnLogin, OpenOnLoginHelp, SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.AutoShowIfDailiesAvailable, value => cfg.AutoShowIfDailiesAvailable = value, "##adt_autoshow"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawListGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(ListGroup);

        var selected = cfg.ExpansionOrder == ExpansionOrder.OldestFirst ? 1 : 0;
        SettingsRow.Draw(OrderLabel, OrderHelp, SettingsControls.RowComboWidth,
            () => SettingsControls.DrawChoices("##adt_order", OrderNames, OrderDetails, selected, choice =>
            {
                cfg.ExpansionOrder = choice == 1 ? ExpansionOrder.OldestFirst : ExpansionOrder.NewestFirst;
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(OrderDetails[selected]);
    }
}
