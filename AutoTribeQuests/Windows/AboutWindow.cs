using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoTribeQuests.Windows;

public sealed class AboutWindow : Window, IDisposable
{
    public AboutWindow() : base("About — Allied Tribes###AutoTribeQuestsAbout")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 220),
            MaximumSize = new Vector2(520, 600),
        };
        Size = new Vector2(380, 260);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        ImGui.SetWindowFontScale(1.30f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted("Allied Tribes");
        ImGui.SetWindowFontScale(1.0f);

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted("Daily allied tribe quests, hands-off.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("Teleports to each tribe issuer, accepts the day's quests, hands them off to Questionable, and turns them in. Crafter tribes also use Artisan via Questionable's internal pipeline.");

        ImGui.Spacing();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextUnformatted("Requires: vnavmesh, Questionable");
            ImGui.TextUnformatted("Optional: Artisan (crafter tribes)");
        }

        ImGui.Spacing();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted("github.com/XeldarAlz/FFXIV-AutoTribeQuests");
    }
}
