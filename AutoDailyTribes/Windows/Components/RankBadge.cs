using AutoDailyTribes.Core;
using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes.Windows.Components;

internal static class RankBadge
{
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
        if (era == TribeEra.ARR && rank >= AdtConstants.MaxTribeRank) return "Allied";
        var idx = rank - 1;
        return idx >= 0 && idx < RankNames.Length ? RankNames[idx] : "";
    }

    public static string RankLabel(TribeInfo tribe)
    {
        if (!tribe.Unlocked) return "Locked";
        var name = RankName(tribe.Rank, tribe.Era);
        return name.Length > 0 ? $"Rank {tribe.Rank} · {name}" : $"Rank {tribe.Rank}";
    }

    public static string RankName(TribeInfo tribe)
        => tribe.Unlocked ? RankName(tribe.Rank, tribe.Era) : "Locked";

    public static (float fraction, bool maxed) Rep(TribeInfo tribe)
    {
        var maxed = tribe.Unlocked && tribe.Rank >= AdtConstants.MaxTribeRank;
        var fraction = maxed
            ? 1f
            : tribe.RepMax > 0 ? Math.Clamp((float)tribe.RepCur / tribe.RepMax, 0f, 1f) : 0f;
        return (fraction, maxed);
    }
}
