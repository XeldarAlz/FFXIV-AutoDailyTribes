using System.Numerics;

namespace AutoTribeQuests.Core.Tribes;

// Static description (id / name / kind / locations) plus mutable live state
// (rank / allowance / accepted-today). Single record class so the UI can data-bind
// to one object per row and the automation coroutine can read+write through it.
public sealed class TribeInfo
{
    // === Static identity ===
    public required uint BeastTribeId { get; init; }
    public required string Name { get; init; }
    public required TribeEra Era { get; init; }
    public required TribeKind Kind { get; init; }
    public required uint UnlockQuestId { get; init; }
    public required int MinRankForDailies { get; init; }
    public required uint IssuerTerritoryId { get; init; }
    public required uint IssuerENpcBaseId { get; init; }
    public uint[] AltIssuerENpcBaseIds { get; init; } = [];

    // Tribes where the daily list is one extra SelectString hop away. Default 0
    // means the addon opens directly on SelectIconString. Override for tribes
    // with side menus (Kojin "Show me what's on offer today", etc.).
    public int IssuerSelectStringIndex { get; init; } = 0;

    // === Resolved on first use ===
    public ulong IssuerInstanceId;
    public Vector3 IssuerLocation;

    // === Live state, refreshed before every Draw / Execute ===
    public bool Unlocked;
    public int Rank;
    public int RepCur, RepMax;
    public int DailyAllowanceLeft;
    public uint[] AlreadyAcceptedToday = [];

    public bool MeetsRankRequirement => Rank >= MinRankForDailies;
    public int AcceptSlotsRemaining => Math.Max(0, Constants.MaxAcceptsPerTribe - AlreadyAcceptedToday.Length);
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
