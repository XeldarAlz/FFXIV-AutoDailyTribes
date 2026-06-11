using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoDailyTribes.Windows.Components;

// Wraps a block of settings in a rounded card. Uses a split draw-list channel so the background can
// be drawn behind variable-height content measured at Dispose. Ported from the sibling plugins.
internal sealed class SettingsGroup : IDisposable
{
    private const float PaddingX = 12f;
    private const float PaddingY = 9f;
    private const float GroupGap = 12f;

    private readonly Vector2 cardOrigin;
    private readonly float cardWidth;

    public static SettingsGroup Begin(string title)
    {
        if (title.Length > 0)
        {
            Styling.SectionLabel(title);
            Styling.VSpace(3f);
        }
        return new SettingsGroup();
    }

    public static SettingsGroup Begin(FontAwesomeIcon icon, string title, Vector4 accent)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, accent))
            ImGui.TextUnformatted(icon.ToIconString());
        ImGui.SameLine(0, 7f * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(title.ToUpperInvariant());
        Styling.VSpace(3f);
        return new SettingsGroup();
    }

    private SettingsGroup()
    {
        var scale = ImGuiHelpers.GlobalScale;
        cardOrigin = ImGui.GetCursorScreenPos();
        cardWidth = ImGui.GetContentRegionAvail().X;

        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);

        ImGui.SetCursorScreenPos(cardOrigin + new Vector2(PaddingX, PaddingY) * scale);
        ImGui.BeginGroup();
        ImGui.PushTextWrapPos(cardOrigin.X + cardWidth - PaddingX * scale);
    }

    public void Dispose()
    {
        ImGui.PopTextWrapPos();
        ImGui.EndGroup();
        var scale = ImGuiHelpers.GlobalScale;
        var cardEnd = new Vector2(cardOrigin.X + cardWidth, ImGui.GetItemRectMax().Y + PaddingY * scale);

        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSetCurrent(0);
        var rounding = Styling.CardRounding * scale;
        drawList.AddRectFilled(cardOrigin, cardEnd, ImGui.GetColorU32(Styling.CardBgSoft), rounding);
        drawList.AddRect(cardOrigin, cardEnd, ImGui.GetColorU32(Styling.WithAlpha(Styling.BorderDim, 0.55f)), rounding);
        drawList.ChannelsMerge();

        ImGui.SetCursorScreenPos(new Vector2(cardOrigin.X, cardEnd.Y));
        ImGui.Dummy(new Vector2(cardWidth, 0f));
        Styling.VSpace(GroupGap);
    }
}
