using Dalamud.Game;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace AutoDailyTribes.Core.Tribes;

public sealed class TribeInfo
{
    public required uint BeastTribeId { get; init; }
    private string fallbackName = string.Empty;
    private string? localizedName;

    // Keep the registry's English name as a fallback. The game's BeastTribe name follows the
    // client language and can include the allied faction after a colon (e.g. 蜥蜴人族：灰党).
    public required string Name
    {
        get
        {
            // The English sheet lowercases several standalone names (sylphs, pixies, etc.).
            // Keep the registry's display casing for the English client.
            if (Svc.Data.Language == ClientLanguage.English) return fallbackName;
            if (localizedName is not null) return localizedName;

            var name = Svc.Data.GetExcelSheet<BeastTribe>()?.GetRowOrDefault(BeastTribeId)?.Name.ToString();
            if (string.IsNullOrWhiteSpace(name)) return fallbackName;

            var factionSeparator = name.IndexOfAny([':', '：']);
            if (factionSeparator > 0) name = name[..factionSeparator].TrimEnd();

            localizedName = name;
            return name;
        }
        init => fallbackName = value;
    }
    public required TribeEra Era { get; init; }
    public required TribeKind Kind { get; init; }
    public required int MinRankForDailies { get; init; }
    public required uint IssuerTerritoryId { get; init; }

    // Every NPC that hands out this tribe's dailies, best-paying first. The ARR tribes have one
    // giver per reputation rank and each offers its own three quests a day; later tribes have one.
    public required uint[] IssuerENpcBaseIds { get; init; }

    public string? IconFile { get; init; }

    public int IssuerSelectStringIndex { get; init; }

    public TribeIssuer[] Issuers = [];

    public bool Unlocked;
    public int Rank;
    public int RepCur, RepMax;
    public int DailyAllowanceLeft;

    // Highest level any daily unlocked at the current rank asks for; 0 when the tribe has none.
    public int RequiredLevel;

    public uint[] InProgressQuestIds = [];

    public int AcceptedTodayCount;

    // A mid-day rank-up refreshes the tribe's three daily offers, but the game keeps the old
    // entries in its daily-done slots until the 15:00 UTC reset — this holds that stale count.
    public int RankCycleBaseline;
    internal int LastSeenRank = -1;
    internal ulong LastSeenCid;
    internal DateTime LastRefreshUtc;

    public bool DailiesRefreshedByRankUp => RankCycleBaseline > 0;

    // The story quest that lifts the tribe past its current rank, 0 unless the reputation sits at
    // the rank cap, the plugin is set to play rank-ups and Questionable can be handed the quest.
    public uint RankUpQuestId;

    public bool CanAutoRankUp => RankUpQuestId != 0;

    public bool MeetsRankRequirement => Rank >= MinRankForDailies;
    public int AcceptSlotsRemaining => Math.Max(0, AdtConstants.MaxAcceptsPerTribe - AcceptedTodayCount);
    public bool HasInProgressQuests => InProgressQuestIds.Length > 0;
    public bool AllSlotsDone => AcceptSlotsRemaining <= 0 && !HasInProgressQuests;

    public bool CanRankUp => Unlocked && Rank < AdtConstants.MaxTribeRank && RepMax > 0 && RepCur >= RepMax;

    public bool HasResolvedIssuers => Issuers.Length > 0;

    public Vector3 CampLocation => Issuers.Length > 0 ? Issuers[0].Location : Vector3.Zero;

    public bool IssuesDaily(uint issuerBaseId) => Array.IndexOf(IssuerENpcBaseIds, issuerBaseId) >= 0;

    private string? cardId;
    private string? kindLabel;

    public string CardId => cardId ??= $"##tribe_{BeastTribeId}";
    public string KindLabel => kindLabel ??= Kind.ToString();
}

public enum TribeEra
{
    ARR,
    HW,
    SB,
    ShB,
    EW,
    DT,
}

public static class TribeEraExtensions
{
    public static string DisplayName(this TribeEra era) => era switch
    {
        TribeEra.ARR => "A Realm Reborn 2.0",
        TribeEra.HW  => "Heavensward 3.0",
        TribeEra.SB  => "Stormblood 4.0",
        TribeEra.ShB => "Shadowbringers 5.0",
        TribeEra.EW  => "Endwalker 6.0",
        TribeEra.DT  => "Dawntrail 7.0",
    };

    public static string ShortName(this TribeEra era) => era switch
    {
        TribeEra.ARR => "A Realm Reborn",
        TribeEra.HW  => "Heavensward",
        TribeEra.SB  => "Stormblood",
        TribeEra.ShB => "Shadowbringers",
        TribeEra.EW  => "Endwalker",
        TribeEra.DT  => "Dawntrail",
    };
}
