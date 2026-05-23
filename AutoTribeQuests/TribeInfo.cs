using System.Numerics;

namespace AutoTribeQuests;

public enum TribeJobKind
{
    Combat,        // ARR Amalj'aa/Sylphs/Kobolds/Sahagin, HW Vanu/Vath, SB Kojin/Ananta, etc.
    Crafter,       // ARR Ixal, HW Moogles, ShB Dwarves
    Gatherer,      // SB Namazu (mixed), DT Pelupelu (TBD)
    Mixed,         // Loporrits, Namazu — handled per-quest
}

public sealed class TribeInfo
{
    public required uint BeastTribeId { get; init; }
    public required string Name { get; init; }
    public required TribeJobKind Kind { get; init; }
    public required uint UnlockQuestId { get; init; }
    public required int MinRankForDailies { get; init; }
    public required uint IssuerTerritoryId { get; init; }
    public required uint IssuerENpcBaseId { get; init; }
    public uint[] AltIssuerENpcBaseIds { get; init; } = [];

    // Per-tribe dialog quirks. Most tribes open the daily list at SelectIconString index 0 ("Accept quest").
    // Some require a different entry option first (e.g. "Show me what's on offer today" then SelectIconString).
    public int IssuerSelectStringIndex { get; init; } = 0;

    // Resolved at first use from territory's planevent.lgb
    public ulong IssuerInstanceId;
    public Vector3 IssuerLocation;

    // Live player state
    public bool Unlocked;
    public int Rank;
    public int RepCur, RepMax;
    public int DailyAllowanceLeft;
    public uint[] AlreadyAcceptedToday = [];

    public bool MeetsRankRequirement => Rank >= MinRankForDailies;
}
