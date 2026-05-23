namespace AutoTribeQuests.Core.Tribes;

// All 18 Allied Tribes (ARR through DT).
//
// Adding a new tribe = add one entry below. Adjusting per-tribe dialog quirks =
// edit IssuerSelectStringIndex / AltIssuerENpcBaseIds.
//
// Every IssuerENpcBaseId marked VERIFY: still needs to be confirmed against the
// live game data. The path of least resistance:
//   1. Travel to the issuer NPC in-game.
//   2. Target it and run /xldata → Object Table → read the BaseId.
//   3. Replace the VERIFY: value here and rebuild.
//
// IssuerResolver logs a warning at runtime if a BaseId doesn't resolve to a real
// EventNPC in the territory's planevent.lgb, so wrong values fail loudly rather
// than producing silent travel-to-nowhere bugs.
public static class TribeRegistry
{
    public static readonly TribeInfo[] Tribes =
    [
        // === ARR ===
        new()
        {
            BeastTribeId = 1,
            Name = "Amalj'aa",
            Era = TribeEra.ARR,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 145,                // Eastern Thanalan
            IssuerENpcBaseId = 1006722,             // VERIFY: Swift
        },
        new()
        {
            BeastTribeId = 2,
            Name = "Sylphs",
            Era = TribeEra.ARR,
            Kind = TribeKind.Combat,
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
            MinRankForDailies = 1,
            IssuerTerritoryId = 154,                // North Shroud
            IssuerENpcBaseId = 1007599,             // VERIFY: Scarlet
        },

        // === HW ===
        new()
        {
            BeastTribeId = 6,
            Name = "Vanu Vanu",
            Era = TribeEra.HW,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 401,                // Sea of Clouds
            IssuerENpcBaseId = 1009196,             // VERIFY: Sonu Vanu
        },
        new()
        {
            BeastTribeId = 7,
            Name = "Vath",
            Era = TribeEra.HW,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 398,                // The Dravanian Forelands
            IssuerENpcBaseId = 1009197,             // VERIFY: Kal Jaagu
        },
        new()
        {
            BeastTribeId = 8,
            Name = "Moogles",
            Era = TribeEra.HW,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 400,                // The Churning Mists
            IssuerENpcBaseId = 1010055,             // VERIFY: Mogmill
        },

        // === SB ===
        new()
        {
            BeastTribeId = 9,
            Name = "Kojin",
            Era = TribeEra.SB,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 613,                // The Ruby Sea
            IssuerENpcBaseId = 1018289,             // VERIFY: Mizuki
            IssuerSelectStringIndex = 0,            // may need entry-menu hop — verify in-game
        },
        new()
        {
            BeastTribeId = 10,
            Name = "Ananta",
            Era = TribeEra.SB,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 612,                // The Fringes
            IssuerENpcBaseId = 1018291,             // VERIFY: Vira
        },
        new()
        {
            BeastTribeId = 11,
            Name = "Namazu",
            Era = TribeEra.SB,
            Kind = TribeKind.Mixed,
            MinRankForDailies = 1,
            IssuerTerritoryId = 614,                // Yanxia
            IssuerENpcBaseId = 1023154,             // VERIFY: Kingu Goishi
            // Namazu's first daily can pop a clan selector — needs per-tribe handling
            // before the SelectIconStringPick(0) call in AutoTribe.Execute.
        },

        // === ShB ===
        new()
        {
            BeastTribeId = 12,
            Name = "Pixie",
            Era = TribeEra.ShB,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 816,                // Il Mheg
            IssuerENpcBaseId = 1027706,             // VERIFY: Wayslan-selan
        },
        new()
        {
            BeastTribeId = 13,
            Name = "Qitari",
            Era = TribeEra.ShB,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 815,                // The Rak'tika Greatwood
            IssuerENpcBaseId = 1027707,             // VERIFY: Boko Hoko
        },
        new()
        {
            BeastTribeId = 14,
            Name = "Dwarves",
            Era = TribeEra.ShB,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 814,                // Kholusia
            IssuerENpcBaseId = 1031820,             // VERIFY: Bzhonk
        },

        // === EW ===
        new()
        {
            BeastTribeId = 15,
            Name = "Arkasodara",
            Era = TribeEra.EW,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 957,                // Thavnair
            IssuerENpcBaseId = 1037551,             // VERIFY: Chamraj
        },
        new()
        {
            BeastTribeId = 16,
            Name = "Omicron",
            Era = TribeEra.EW,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 961,                // Ultima Thule
            IssuerENpcBaseId = 1043879,             // VERIFY: Geulla
        },
        new()
        {
            BeastTribeId = 17,
            Name = "Loporrits",
            Era = TribeEra.EW,
            Kind = TribeKind.Mixed,
            MinRankForDailies = 1,
            IssuerTerritoryId = 959,                // Mare Lamentorum
            IssuerENpcBaseId = 1042881,             // VERIFY: Cherubeloff
            // Loporrits hub has multiple issuer NPCs — once a primary is verified,
            // add the others to AltIssuerENpcBaseIds.
        },

        // === DT ===
        new()
        {
            BeastTribeId = 18,
            Name = "Pelupelu",
            Era = TribeEra.DT,
            Kind = TribeKind.Combat,                // Patch 7.1 — verify in-game
            MinRankForDailies = 1,
            IssuerTerritoryId = 1184,               // VERIFY: Tuliyollal/Solution Nine region
            IssuerENpcBaseId = 1052000,             // VERIFY: placeholder — needs in-game lookup
            // Verify Questionable has quest data coverage before enabling this row in earnest.
        },
        new()
        {
            BeastTribeId = 19,
            Name = "Mamool Ja",
            Era = TribeEra.DT,
            Kind = TribeKind.Crafter,               // Patch 7.2 DoH crafter tribe
            MinRankForDailies = 1,
            IssuerTerritoryId = 1188,               // VERIFY: Yak T'el or adjacent
            IssuerENpcBaseId = 1052100,             // VERIFY: placeholder — needs in-game lookup
        },
        new()
        {
            BeastTribeId = 20,
            Name = "Yok Huy",
            Era = TribeEra.DT,
            Kind = TribeKind.Gatherer,              // Patch 7.3 DoL gatherer tribe
            MinRankForDailies = 1,
            IssuerTerritoryId = 1189,               // VERIFY: Shaaloani
            IssuerENpcBaseId = 1052200,             // VERIFY: placeholder — needs in-game lookup
        },
    ];

    public static IEnumerable<TribeInfo> ByEra(TribeEra era) => Tribes.Where(t => t.Era == era);
}
