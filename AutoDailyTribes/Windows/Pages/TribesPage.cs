using AutoDailyTribes.Windows.Sections;
using AutoDailyTribes.Windows.Shell;

namespace AutoDailyTribes.Windows.Pages;

internal sealed class TribesPage
{
    private const float SwitchRevealMs = 320f;

    public void Draw(Plugin plugin, AppWindow window)
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;

        using var reveal = Motion.PushSwitch("##adt_tribes_state", ctrl.Running, SwitchRevealMs);
        if (ctrl.Running)
        {
            RunningPanel.Draw(cfg, ctrl);
            return;
        }

        if (Headline.Draw(cfg, ctrl)) window.Show(AppWindow.Page.Plugins);
        Styling.VSpace(20f);

        TodayCard.Draw(cfg, ctrl);
        Styling.VSpace(26f);

        TribeLibrary.Draw(cfg, ctrl);
        Styling.VSpace(12f);
    }
}
