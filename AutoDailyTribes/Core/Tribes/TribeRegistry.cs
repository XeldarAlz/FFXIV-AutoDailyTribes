namespace AutoDailyTribes.Core.Tribes;

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
            MinRankForDailies = 1,
            IssuerTerritoryId = 146,                // Southern Thanalan
            IssuerENpcBaseIds = [1005552, 1005551, 1005550], // Yadovv Gah (Friendly), Narujj Boh (Recognized), Fibubb Gah (Neutral)
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
            IssuerENpcBaseIds = [1005563, 1005562, 1005561], // Moxia (Friendly), Ponnixia (Recognized), Tonaxia (Neutral)
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
            IssuerENpcBaseIds = [1005930, 1005929, 1005928], // 789th Order Dustman Bo Bu (Friendly), Craftsman Bo Gu (Recognized), Dustman Bo Zu (Neutral)
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
            IssuerENpcBaseIds = [1005940, 1005939, 1005938], // Seww (Friendly), Houu (Recognized), Fyuu (Neutral)
            IconFile = "Sahagin_Relations.png",
        },
        new()
        {
            BeastTribeId = 5,
            Name = "Ixal",
            Era = TribeEra.ARR,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 154,                // North Shroud
            IssuerENpcBaseIds = [1009216, 1009215, 1009214, 1009213, 1009212, 1009211],
            // Jezul Ahuatan the Second (Honored), Duzal Meyean the Steady (Respected), Tazel Meyean the Lettered (Trusted),
            // Rozol Cattlan the Prudent (Friendly), Methuli Cattlan the Hard (Recognized), Yazel Ahuatan the Able (Neutral)
            IconFile = "Ixali_Relations.png",
        },
        new()
        {
            BeastTribeId = 6,
            Name = "Vanu Vanu",
            Era = TribeEra.HW,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 401,                // Sea of Clouds
            IssuerENpcBaseIds = [1016089],           // Muna Vanu
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
            IssuerENpcBaseIds = [1016803],           // Vath keeneye
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
            IssuerENpcBaseIds = [1017171],           // Mogek the Marvelous
            IconFile = "Moogle_Relations.png",
        },
        new()
        {
            BeastTribeId = 9,
            Name = "Kojin",
            Era = TribeEra.SB,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 613,                // The Ruby Sea
            IssuerENpcBaseIds = [1024217],           // Zukin
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
            IssuerENpcBaseIds = [1024773],           // Eshana
            IconFile = "Ananta_Relations.png",
        },
        new()
        {
            BeastTribeId = 11,
            Name = "Namazu",
            Era = TribeEra.SB,
            Kind = TribeKind.Mixed,
            MinRankForDailies = 1,
            IssuerTerritoryId = 622,                // The Azim Steppe
            IssuerENpcBaseIds = [1025602],           // Seigetsu the Enlightened
            IconFile = "Namazu_Relations.png",
        },
        new()
        {
            BeastTribeId = 12,
            Name = "Pixie",
            Era = TribeEra.ShB,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 816,                // Il Mheg
            IssuerENpcBaseIds = [1031809],           // Uin Nee
            IconFile = "Dreamspinners_Relations.png",
        },
        new()
        {
            BeastTribeId = 13,
            Name = "Qitari",
            Era = TribeEra.ShB,
            Kind = TribeKind.Gatherer,
            MinRankForDailies = 1,
            IssuerTerritoryId = 817,                // The Rak'tika Greatwood
            IssuerENpcBaseIds = [1032643],           // Qhoterl Pasol
            IconFile = "Stewards_Relations.png",
        },
        new()
        {
            BeastTribeId = 14,
            Name = "Dwarves",
            Era = TribeEra.ShB,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 813,                // Lakeland
            IssuerENpcBaseIds = [1033712],           // Regitt
            IconFile = "Dwarf_Relations.png",
        },
        new()
        {
            BeastTribeId = 15,
            Name = "Arkasodara",
            Era = TribeEra.EW,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 957,                // Thavnair
            IssuerENpcBaseIds = [1042301],           // Maru
            IconFile = "Arkasodara_Relations.png",
        },
        new()
        {
            BeastTribeId = 16,
            Name = "Omicron",
            Era = TribeEra.EW,
            Kind = TribeKind.Gatherer,
            MinRankForDailies = 1,
            IssuerTerritoryId = 960,                // Ultima Thule
            IssuerENpcBaseIds = [1043417],           // Stigma-4
            IconFile = "Omicron_Relations.png",
        },
        new()
        {
            BeastTribeId = 17,
            Name = "Loporrits",
            Era = TribeEra.EW,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 959,                // Mare Lamentorum
            IssuerENpcBaseIds = [1044403],           // Managingway
            IconFile = "Loporrit_Relations.png",
        },
        new()
        {
            BeastTribeId = 18,
            Name = "Pelupelu",
            Era = TribeEra.DT,
            Kind = TribeKind.Combat,
            MinRankForDailies = 1,
            IssuerTerritoryId = 1188,               // Kozama'uka
            IssuerENpcBaseIds = [1051711],           // Yubli
            IconFile = "Pelupelu_Relations.png",
        },
        new()
        {
            BeastTribeId = 19,
            Name = "Mamool Ja",
            Era = TribeEra.DT,
            Kind = TribeKind.Gatherer,
            MinRankForDailies = 1,
            IssuerTerritoryId = 1189,               // Yak T'el
            IssuerENpcBaseIds = [1052560],           // Kageel Ja
            IconFile = "Mamool_Ja_Relations.png",
        },
        new()
        {
            BeastTribeId = 20,
            Name = "Yok Huy",
            Era = TribeEra.DT,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 1187,               // Urqopacha
            IssuerENpcBaseIds = [1054635],           // Vuyargur
            IconFile = "Yok_Huy_Relations.png",
        },
    ];

    private static readonly TribeInfo[][] EraBuckets = BuildEraBuckets();

    public static TribeInfo[] ByEra(TribeEra era) => EraBuckets[(int)era];

    private static TribeInfo[][] BuildEraBuckets()
    {
        var eraCount = Enum.GetValues<TribeEra>().Length;
        var counts = new int[eraCount];
        for (var tribeIndex = 0; tribeIndex < Tribes.Length; tribeIndex++)
        {
            counts[(int)Tribes[tribeIndex].Era]++;
        }

        var buckets = new TribeInfo[eraCount][];
        for (var eraIndex = 0; eraIndex < eraCount; eraIndex++)
        {
            buckets[eraIndex] = new TribeInfo[counts[eraIndex]];
        }

        var filled = new int[eraCount];
        for (var tribeIndex = 0; tribeIndex < Tribes.Length; tribeIndex++)
        {
            var eraIndex = (int)Tribes[tribeIndex].Era;
            buckets[eraIndex][filled[eraIndex]++] = Tribes[tribeIndex];
        }

        return buckets;
    }
}
