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
            IssuerTerritoryId = 145,                // Eastern Thanalan
            IssuerENpcBaseId = 1005550,             // Swift
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
            IssuerENpcBaseId = 1005561,             // Komuxio
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
            IssuerENpcBaseId = 1005928,             // Drekkenfrau
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
            IssuerENpcBaseId = 1005938,             // Novv
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
            IssuerENpcBaseId = 1009211,             // Yazel Ahuatan the Able
            AltIssuerENpcBaseIds = [1009214],       // Tazel Meyean the Lettered
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
            IssuerENpcBaseId = 1016089,             // Muna Vanu
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
            IssuerENpcBaseId = 1016803,             // Vath Keeneye
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
            IssuerENpcBaseId = 1017171,             // Mogmill
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
            IssuerENpcBaseId = 1024217,             // Zukin
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
            IssuerENpcBaseId = 1024773,             // Eshana
            IconFile = "Ananta_Relations.png",
        },
        new()
        {
            BeastTribeId = 11,
            Name = "Namazu",
            Era = TribeEra.SB,
            Kind = TribeKind.Mixed,
            MinRankForDailies = 1,
            IssuerTerritoryId = 622,                // Yanxia
            IssuerENpcBaseId = 1025602,             // Seigetsu the Enlightened
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
            IssuerENpcBaseId = 1031809,             // Uin Nee
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
            IssuerENpcBaseId = 1032643,             // Qhoterl Pasol
            IconFile = "Stewards_Relations.png",
        },
        new()
        {
            BeastTribeId = 14,
            Name = "Dwarves",
            Era = TribeEra.ShB,
            Kind = TribeKind.Crafter,
            MinRankForDailies = 1,
            IssuerTerritoryId = 813,                // Kholusia
            IssuerENpcBaseId = 1033712,             // Regitt
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
            IssuerENpcBaseId = 1042301,             // Maru
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
            IssuerENpcBaseId = 1043417,             // Stigma-4
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
            IssuerENpcBaseId = 1044403,             // Managingway
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
            IssuerENpcBaseId = 1051711,             // Yubli
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
            IssuerENpcBaseId = 1052560,             // Kageel Ja
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
            IssuerENpcBaseId = 1054635,             // Vuyargur
            IconFile = "Yok_Huy_Relations.png",
        },
    ];

    public static readonly (TribeEra Era, TribeInfo[] Members)[] ErasNewestFirst = BuildErasNewestFirst();

    private static (TribeEra, TribeInfo[])[] BuildErasNewestFirst()
    {
        var eras = Enum.GetValues<TribeEra>();
        var groups = new List<(TribeEra, TribeInfo[])>(eras.Length);
        for (var index = eras.Length - 1; index >= 0; index--)
        {
            var era = eras[index];
            var members = Array.FindAll(Tribes, tribe => tribe.Era == era);
            if (members.Length > 0) groups.Add((era, members));
        }
        return groups.ToArray();
    }
}
