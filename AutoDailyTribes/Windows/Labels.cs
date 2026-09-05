using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes.Windows;

// Display names for the game's own taxonomy. Tribe names themselves stay as the registry spells
// them, matching how the sibling plugins leave game data to the client.
internal static class Labels
{
    public static string Kind(TribeKind kind) => Loc.T(kind switch
    {
        TribeKind.Combat   => L.Kind.Combat,
        TribeKind.Crafter  => L.Kind.Crafter,
        TribeKind.Gatherer => L.Kind.Gatherer,
        _                  => L.Kind.Mixed,
    });

    public static string Era(TribeEra era) => Loc.T(era switch
    {
        TribeEra.ARR => L.Era.Arr,
        TribeEra.HW  => L.Era.Hw,
        TribeEra.SB  => L.Era.Sb,
        TribeEra.ShB => L.Era.Shb,
        TribeEra.EW  => L.Era.Ew,
        _            => L.Era.Dt,
    });
}
