using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Localization;
using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes.Windows.Components;

internal static class RankBadge
{
    private static readonly LocString[] RankNames =
    [
        L.Rank.Neutral,
        L.Rank.Recognized,
        L.Rank.Friendly,
        L.Rank.Trusted,
        L.Rank.Respected,
        L.Rank.Honored,
        L.Rank.Sworn,
        L.Rank.Bloodsworn,
    ];

    // ARR-era societies finish at "Allied"; later eras use "Bloodsworn".
    private static string RankName(int rank, TribeEra era)
    {
        if (era == TribeEra.ARR && rank >= AdtConstants.MaxTribeRank) return Loc.T(L.Rank.Allied);
        var index = rank - 1;
        return index >= 0 && index < RankNames.Length ? Loc.T(RankNames[index]) : string.Empty;
    }

    public static string RankLabel(TribeInfo tribe)
    {
        if (!tribe.Unlocked) return Loc.T(L.Rank.Locked);
        var name = RankName(tribe.Rank, tribe.Era);
        return name.Length > 0 ? Loc.T(L.Rank.NamedLabel, tribe.Rank, name) : Loc.T(L.Rank.Numbered, tribe.Rank);
    }

    public static string RankName(TribeInfo tribe)
        => tribe.Unlocked ? RankName(tribe.Rank, tribe.Era) : Loc.T(L.Rank.Locked);

    public static (float Fraction, bool Maxed) Rep(TribeInfo tribe)
    {
        var maxed = tribe.Unlocked && tribe.Rank >= AdtConstants.MaxTribeRank;
        var fraction = maxed
            ? 1f
            : tribe.RepMax > 0 ? Math.Clamp((float)tribe.RepCur / tribe.RepMax, 0f, 1f) : 0f;
        return (fraction, maxed);
    }
}
