using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes.Windows.Components;

internal static class RankBadge
{
    private const string LockedLabel = "Locked";
    private const string AlliedName = "Allied";

    private static readonly string[] RankNames =
    [
        "Neutral",
        "Recognized",
        "Friendly",
        "Trusted",
        "Respected",
        "Honored",
        "Sworn",
        "Bloodsworn",
    ];

    // ARR-era societies finish at "Allied"; later eras use "Bloodsworn".
    private static string RankName(int rank, TribeEra era)
    {
        if (era == TribeEra.ARR && rank >= AdtConstants.MaxTribeRank) return AlliedName;
        var index = rank - 1;
        return index >= 0 && index < RankNames.Length ? RankNames[index] : string.Empty;
    }

    public static string RankLabel(TribeInfo tribe)
    {
        if (!tribe.Unlocked) return LockedLabel;
        var name = RankName(tribe.Rank, tribe.Era);
        return name.Length > 0 ? $"Rank {tribe.Rank} · {name}" : $"Rank {tribe.Rank}";
    }

    public static string RankName(TribeInfo tribe)
        => tribe.Unlocked ? RankName(tribe.Rank, tribe.Era) : LockedLabel;

    public static (float Fraction, bool Maxed) Rep(TribeInfo tribe)
    {
        var maxed = tribe.Unlocked && tribe.Rank >= AdtConstants.MaxTribeRank;
        var fraction = maxed
            ? 1f
            : tribe.RepMax > 0 ? Math.Clamp((float)tribe.RepCur / tribe.RepMax, 0f, 1f) : 0f;
        return (fraction, maxed);
    }
}
