using System.Numerics;

namespace AutoDailyTribes.Core.Tribes;

public sealed class TribeInfo
{
    public required uint BeastTribeId { get; init; }
    public required string Name { get; init; }
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
