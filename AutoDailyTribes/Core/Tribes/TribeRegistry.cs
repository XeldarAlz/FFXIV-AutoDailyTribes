namespace AutoDailyTribes.Core.Tribes;

// VERIFY: each IssuerENpcBaseId is a placeholder until confirmed in-game.
// Stand next to the issuer NPC, target it, run /adt target, paste the BaseId.
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
            IconFile = "Amalj'aa_Relations.png",
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
            IconFile = "Sylphic_Relations.png",
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
            IconFile = "Kobold_Relations.png",
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
            IconFile = "Sahagin_Relations.png",
        },
        new()
        {
            BeastTribeId = 5,
            Name = "Ixal",
            Era = TribeEra.ARR,
            Kind = TribeKind.Crafter,               // DoH-only daily focus (rank-1 unlock @ Ehcatl, North Shroud)
            MinRankForDailies = 1,
            IssuerTerritoryId = 154,                // North Shroud
            IssuerENpcBaseId = 1007599,             // VERIFY: Scarlet
            IconFile = "Ixali_Relations.png",
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
            IconFile = "Vanu_Relations.png",
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
            IconFile = "Vath_Relations.png",
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
            IconFile = "Moogle_Relations.png",
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
            IconFile = "Kojin_Relations.png",
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
            IconFile = "Ananta_Relations.png",
        },
        new()
        {
            BeastTribeId = 11,
            Name = "Namazu",
            Era = TribeEra.SB,
            Kind = TribeKind.Mixed,                 // DoH + DoL — verified
            MinRankForDailies = 1,
            IssuerTerritoryId = 614,                // Yanxia
            IssuerENpcBaseId = 1023154,             // VERIFY: Kingu Goishi
            // Namazu's first daily can pop a clan selector — needs per-tribe handling
            // before the SelectIconStringPick(0) call in AutoTribe.Execute.
            IconFile = "Namazu_Relations.png",
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
            IconFile = "Dreamspinners_Relations.png",   // in-game Pixie tribe is "Dreamspinners"
        },
        new()
        {
            BeastTribeId = 13,
            Name = "Qitari",
            Era = TribeEra.ShB,
            Kind = TribeKind.Gatherer,              // DoL focus — verified
            MinRankForDailies = 1,
            IssuerTerritoryId = 817,                // The Rak'tika Greatwood (was 815 = Amh Araeng)
            IssuerENpcBaseId = 1027707,             // VERIFY: Boko Hoko
            IconFile = "Stewards_Relations.png",    // in-game Qitari society is "Stewards"
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
            IconFile = "Dwarf_Relations.png",
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
            IconFile = "Arkasodara_Relations.png",
        },
        new()
        {
            BeastTribeId = 16,
            Name = "Omicron",
            Era = TribeEra.EW,
            Kind = TribeKind.Gatherer,              // DoL focus — verified
            MinRankForDailies = 1,
            IssuerTerritoryId = 960,                // Ultima Thule (was 961 = Elpis)
            IssuerENpcBaseId = 1043879,             // VERIFY: Geulla
            IconFile = "Omicron_Relations.png",
        },
        new()
        {
            BeastTribeId = 17,
            Name = "Loporrits",
            Era = TribeEra.EW,
            Kind = TribeKind.Crafter,               // DoH focus — verified
            MinRankForDailies = 1,
            IssuerTerritoryId = 959,                // Mare Lamentorum
            IssuerENpcBaseId = 1042881,             // VERIFY: Cherubeloff
            // Loporrits hub has multiple issuer NPCs — once a primary is verified,
            // add the others to AltIssuerENpcBaseIds.
            IconFile = "Loporrit_Relations.png",
        },

        // === DT ===
        // Territory IDs verified against xivapi/ffxiv-datamining TerritoryType.csv:
        //   1187 = Urqopacha, 1188 = Kozama'uka, 1189 = Yak T'el, 1190 = Shaaloani.
        // (1186 is Solution Nine — earlier values were off-by-one, hence the
        //  "teleports to Solution Nine then walks to a random NPC" bug.)
        // IssuerENpcBaseId still needs in-game capture for each. Use /adt target
        // while standing next to the issuer to log its real BaseId.
        new()
        {
            BeastTribeId = 18,
            Name = "Pelupelu",
            Era = TribeEra.DT,
            Kind = TribeKind.Combat,                // DoW/DoM focus — Patch 7.1
            MinRankForDailies = 1,
            IssuerTerritoryId = 1188,               // Kozama'uka — Dock Poga (X:37.2, Y:16.8)
            IssuerENpcBaseId = 1052000,             // VERIFY: capture with /adt target
            IconFile = "Pelupelu_Relations.png",
        },
        new()
        {
            BeastTribeId = 19,
            Name = "Mamool Ja",
            Era = TribeEra.DT,
            Kind = TribeKind.Gatherer,              // DoL focus — Patch 7.25
            MinRankForDailies = 1,
            IssuerTerritoryId = 1189,               // Yak T'el — Gok Golma (X:33.2, Y:36.0)
            IssuerENpcBaseId = 1052560,             // Kageel Ja (verified in-game 2026-05-23)
            IconFile = "Mamool_Ja_Relations.png",
        },
        new()
        {
            BeastTribeId = 20,
            Name = "Yok Huy",
            Era = TribeEra.DT,
            Kind = TribeKind.Crafter,               // DoH focus — Patch 7.35
            MinRankForDailies = 1,
            IssuerTerritoryId = 1187,               // Urqopacha — Solace (X:31, Y:37)
            IssuerENpcBaseId = 1052200,             // VERIFY: capture with /adt target
            IconFile = "Yok_Huy_Relations.png",
        },
    ];

    public static IEnumerable<TribeInfo> ByEra(TribeEra era) => Tribes.Where(t => t.Era == era);
}
