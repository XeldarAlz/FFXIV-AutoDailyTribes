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

    // Filename under Images/Tribes/. Null falls back to the FontAwesome KindIcon.
    public string? IconFile { get; init; }

    // Index into the entry-menu SelectString for tribes with an extra hop. 0 = daily list opens directly.
    public int IssuerSelectStringIndex { get; init; } = 0;

    public ulong IssuerInstanceId;
    public Vector3 IssuerLocation;

    public bool Unlocked;
    public int Rank;
    public int RepCur, RepMax;
    public int DailyAllowanceLeft;

    // Accepted-but-not-turned-in journal quests, full 0x10000|id form (what Questionable expects masked to 16-bit).
    public uint[] InProgressQuestIds = [];

    // Daily slots consumed for this tribe today including turn-ins (which drop out of InProgressQuestIds).
    public int AcceptedTodayCount;

    public bool MeetsRankRequirement => Rank >= MinRankForDailies;
    public int AcceptSlotsRemaining => Math.Max(0, AdtConstants.MaxAcceptsPerTribe - AcceptedTodayCount);
    public bool HasInProgressQuests => InProgressQuestIds.Length > 0;

    // Rep bar capped at the threshold for the next rank, awaiting the issuer's rank-up quest.
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
        _ => era.ToString(),
    };
}
