namespace AutoTribeQuests.Core.Tribes;

// Adding a new tribe = add one entry below, optionally tweak
// IssuerSelectStringIndex / AltIssuerENpcBaseIds for per-tribe dialog quirks.
//
// Every uint marked VERIFY needs to be confirmed against the Lumina sheets:
//   BeastTribe          - tribe metadata (BeastTribeId)
//   BeastTribeQuest     - the daily quest IDs per tribe
//   ENpcResident        - NPC names → IssuerENpcBaseId
//   Quest               - intro quest IDs → UnlockQuestId
//   TerritoryType       - zone IDs → IssuerTerritoryId
public static class TribeRegistry
{
    public static readonly TribeInfo[] Tribes =
    [
        new()
        {
            BeastTribeId = 1,
            Name = "Amalj'aa",
            Era = TribeEra.ARR,
            Kind = TribeKind.Combat,
            UnlockQuestId = 65602,                  // VERIFY: "Peace for Thanalan"
            MinRankForDailies = 1,
            IssuerTerritoryId = 145,                // Eastern Thanalan
            IssuerENpcBaseId = 1006722,             // VERIFY: Bartholomew
        },
        new()
        {
            BeastTribeId = 2,
            Name = "Sylphs",
            Era = TribeEra.ARR,
            Kind = TribeKind.Combat,
            UnlockQuestId = 65741,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 152,                // East Shroud
            IssuerENpcBaseId = 1006821,             // VERIFY: Komuxio
        },
        new()
        {
            BeastTribeId = 3,
            Name = "Kobolds",
            Era = TribeEra.ARR,
            Kind = TribeKind.Combat,
            UnlockQuestId = 65643,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 180,                // Outer La Noscea
            IssuerENpcBaseId = 1006747,             // VERIFY: Drekkenfrau
        },
        new()
        {
            BeastTribeId = 4,
            Name = "Sahagin",
            Era = TribeEra.ARR,
            Kind = TribeKind.Combat,
            UnlockQuestId = 65644,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 138,                // Western La Noscea
            IssuerENpcBaseId = 1006750,             // VERIFY: Novv
        },
        new()
        {
            BeastTribeId = 5,
            Name = "Ixal",
            Era = TribeEra.ARR,
            Kind = TribeKind.Crafter,
            UnlockQuestId = 65970,                  // VERIFY
            MinRankForDailies = 1,
            IssuerTerritoryId = 154,                // North Shroud
            IssuerENpcBaseId = 1007599,             // VERIFY: Scarlet
        },

        // TODO HW: Vanu Vanu (territory 399), Vath (400), Moogles (401)
        // TODO SB: Kojin (613, SelectString hop), Ananta (614), Namazu (622, clan selector)
        // TODO ShB: Pixies (816), Qitari (818), Dwarves (820)
        // TODO EW: Arkasodara (957), Omicrons (958), Loporrits (959)
        // TODO DT: Pelupelu (1191) - verify Questionable coverage
    ];

    public static IEnumerable<TribeInfo> ByEra(TribeEra era) => Tribes.Where(t => t.Era == era);
}
