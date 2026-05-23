using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using System.Diagnostics;
using System.Numerics;

namespace AutoDailyTribes.Windows;

public sealed class AboutWindow : Window, IDisposable
{
    private const string RepoUrl = "https://github.com/XeldarAlz/FFXIV-AutoDailyTribes";
    private const string IssuesUrl = "https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/issues";
    private const string DiscussionsUrl = "https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/discussions";
    private const string SecurityUrl = "https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/security/advisories/new";
    private const string Author = "XeldarAlz";
    private const string License = "AGPL-3.0-or-later";

    public AboutWindow() : base("Auto Daily Tribes — About###AutoDailyTribesAbout")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(560, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        DrawHeader();
        ImGui.Separator();
        ImGui.Spacing();
        DrawDetailsTable();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawDescription();
    }

    private static void DrawHeader()
    {
        ImGui.SetWindowFontScale(1.20f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted("Auto Daily Tribes");
        ImGui.SetWindowFontScale(1.0f);

        var version = typeof(AboutWindow).Assembly.GetName().Version?.ToString() ?? "?";
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"build {version} · {License}");
    }

    private static void DrawDetailsTable()
    {
        if (!ImGui.BeginTable("##about_table", 2,
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.PadOuterX))
            return;

        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, 150f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

        DrawTextRow("Author", Author);
        DrawLinkRow("GitHub", RepoUrl);
        DrawLinkRow("Report a bug", IssuesUrl);
        DrawLinkRow("Discussions", DiscussionsUrl);
        DrawLinkRow("Security disclosure", SecurityUrl);

        ImGui.EndTable();
    }

    private static void DrawDescription()
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(
                "Auto Daily Tribes lists every beast tribe from A Realm Reborn through Dawntrail. " +
                "Tick the tribes you want to run, click \"Run selected\" in the header, and the " +
                "plugin teleports to each issuer in turn, accepts the day's quests, hands them " +
                "off to Questionable, and turns them in. The 12-quest daily allowance stops " +
                "the queue automatically once it's burnt.");
            ImGui.PopTextWrapPos();
        }
    }

    private static void DrawTextRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(label);
        ImGui.TableSetColumnIndex(1);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(value);
    }

    private static void DrawLinkRow(string label, string url)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(label);

        ImGui.TableSetColumnIndex(1);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentTeal))
        {
            ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
            ImGui.TextUnformatted(url);
            ImGui.PopTextWrapPos();
        }
        if (!ImGui.IsItemHovered()) return;

        using (ImRaii.Tooltip())
            ImGui.TextUnformatted("Click to open · right-click to copy");
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) OpenInBrowser(url);
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(url);
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[AutoDailyTribes] failed to launch browser for {0}, copied to clipboard instead", url);
            ImGui.SetClipboardText(url);
        }
    }
}
