using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace AutoTribeQuests;

public class Service
{
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static ICondition Conditions { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static ITargetManager Targets { get; private set; } = null!;

    public static Lumina.GameData LuminaGameData => DataManager.GameData;
    public static Lumina.Excel.ExcelSheet<T>? LuminaSheet<T>() where T : struct, Lumina.Excel.IExcelRow<T>
        => LuminaGameData?.GetExcelSheet<T>(Lumina.Data.Language.English);
    public static Lumina.Excel.SubrowExcelSheet<T>? LuminaSheetSubrow<T>() where T : struct, Lumina.Excel.IExcelSubrow<T>
        => LuminaGameData?.GetSubrowExcelSheet<T>(Lumina.Data.Language.English);
    public static T? LuminaRow<T>(uint row) where T : struct, Lumina.Excel.IExcelRow<T>
        => LuminaSheet<T>()?.GetRowOrDefault(row);
    public static Lumina.Excel.SubrowCollection<T>? LuminaSubrows<T>(uint row) where T : struct, Lumina.Excel.IExcelSubrow<T>
        => LuminaSheetSubrow<T>()?.GetRowOrDefault(row);
    public static T? LuminaRow<T>(uint row, ushort subRow) where T : struct, Lumina.Excel.IExcelSubrow<T>
        => LuminaSheetSubrow<T>()?.GetSubrowOrDefault(row, subRow);

    internal static void Initialize(Plugin plugin, IDalamudPluginInterface dalamud)
    {
        dalamud.Create<Service>();
    }
}
