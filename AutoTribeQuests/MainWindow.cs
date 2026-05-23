using clib.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoTribeQuests;

public unsafe class MainWindow : Window, IDisposable
{
    public MainWindow() : base("Allied Tribes")
    {
        TitleBarButtons.Add(new() { Icon = FontAwesomeIcon.Cog, IconOffset = new(1), Click = _ => ImGui.OpenPopup("###config") });
        SizeConstraints = new() { MinimumSize = new(560, 200), MaximumSize = new(1200, 900) };
    }

    public void Dispose() { }

    public override void PreOpenCheck()
    {
        if (!Plugin.Config.AutoShowIfDailiesAvailable) return;
        if (!UIState.Instance()->PlayerState.IsLoaded) return;
        // TODO: open automatically only when at least one tribe has dailies available
    }

    public override void Draw()
    {
        DrawConfigPopup();
        DrawHeader();
        DrawTribeTable();
    }

    private void DrawConfigPopup()
    {
        using var popup = ImRaii.Popup("###config");
        if (popup)
            Plugin.Config.Draw();
    }

    private void DrawHeader()
    {
        using (ImRaii.Disabled(!Svc.Automation.Running))
            if (ImGui.Button("Stop"))
                Svc.Automation.Stop();
        ImGui.SameLine();
        ImGui.TextUnformatted($"Status: {Svc.Automation.CurrentTask?.Status ?? "idle"}");

        // TODO: global allowance read
        ImGui.SameLine();
        ImGui.TextDisabled("  |  Daily allowance: ?? / 12");
    }

    private void DrawTribeTable()
    {
        using var table = ImRaii.Table("tribes", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn("Tribe", ImGuiTableColumnFlags.WidthFixed, 130);
        ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Today", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Actions");
        ImGui.TableHeadersRow();

        foreach (var tribe in TribeData.Tribes)
        {
            using var id = ImRaii.PushId((int)tribe.BeastTribeId);
            TribeData.RefreshLiveState(tribe);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(tribe.Name);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(tribe.Kind.ToString());

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(tribe.Unlocked ? $"{tribe.Rank}" : "locked");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{tribe.AlreadyAcceptedToday.Length}/3");

            ImGui.TableNextColumn();
            var runnable = TribeData.IsAvailableNow(tribe) && !Svc.Automation.Running;
            using (ImRaii.Disabled(!runnable))
                if (ImGui.Button("Do dailies"))
                    Svc.Automation.Start(new AutoTribe(tribe));
        }
    }
}
