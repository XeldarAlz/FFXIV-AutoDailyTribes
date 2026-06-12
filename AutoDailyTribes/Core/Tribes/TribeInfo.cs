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
    public required uint IssuerENpcBaseId { get; init; }
    public uint[] AltIssuerENpcBaseIds { get; init; } = [];

    public string? IconFile { get; init; }

    public int IssuerSelectStringIndex { get; init; }

    public ulong IssuerInstanceId;
    public Vector3 IssuerLocation;

    public bool Unlocked;
    public int Rank;
    public int RepCur, RepMax;
    public int DailyAllowanceLeft;

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
}
