using AutoDailyTribes.Core;
using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Tribes;
using Dalamud.Bindings.ImGui;

namespace AutoDailyTribes.Windows.Sections;

// One per-frame snapshot of what a run would do right now, shared by the headline, the today card,
// the dock and the header pill so no two of them ever disagree about the counts. The runnable list
// follows the order the tribes were picked in, which is the order the batch visits them.
internal static class RunPlan
{
    public sealed class Snapshot
    {
        public readonly List<TribeInfo> Runnable = [];
        public int SelectedCount;
        public int AllowanceLeft;
        public int AllowancesNeeded;
        public bool Exhausted;
        public bool DependenciesReady;

        public bool CanRun => Runnable.Count > 0 && DependenciesReady;
    }

    private static readonly Snapshot snapshot = new();
    private static readonly Dictionary<uint, TribeInfo> byId = BuildIndex();
    private static int cachedFrame = -1;

    public static Snapshot Resolve(Configuration cfg)
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedFrame) return snapshot;

        cachedFrame = frame;
        Compute(cfg);
        return snapshot;
    }

    public static bool IsRunnable(TribeInfo tribe)
        => tribe.Unlocked && tribe.MeetsRankRequirement && (tribe.AcceptSlotsRemaining > 0 || tribe.HasInProgressQuests);

    public static bool IsMaxed(TribeInfo tribe) => tribe.Unlocked && tribe.Rank >= AdtConstants.MaxTribeRank;

    // The header chips hide tribes from the grid and from the run alike, so one predicate decides both.
    public static bool PassesFilters(Configuration cfg, TribeInfo tribe)
    {
        if (cfg.HiddenKinds.Contains(tribe.Kind)) return false;
        return !cfg.HideMaxedTribes || !IsMaxed(tribe);
    }

    public static TribeInfo? Find(uint beastTribeId) => byId.GetValueOrDefault(beastTribeId);

    public static void RefreshAll()
    {
        var tribes = TribeRegistry.Tribes;
        for (var index = 0; index < tribes.Length; index++) TribeStateReader.Refresh(tribes[index]);
    }

    private static void Compute(Configuration cfg)
    {
        RefreshAll();

        snapshot.Runnable.Clear();
        snapshot.AllowanceLeft = TribeStateReader.GlobalAllowanceLeft();
        snapshot.Exhausted = snapshot.AllowanceLeft <= 0;
        snapshot.DependenciesReady = ExternalPlugins.AllRequiredInstalled();
        snapshot.SelectedCount = 0;
        snapshot.AllowancesNeeded = 0;

        var selected = cfg.SelectedTribes;
        var budget = snapshot.AllowanceLeft;
        for (var index = 0; index < selected.Count; index++)
        {
            var tribe = Find(selected[index]);
            if (tribe is null) continue;

            snapshot.SelectedCount++;
            if (!IsRunnable(tribe) || !PassesFilters(cfg, tribe)) continue;
            if (!tribe.HasInProgressQuests && (tribe.AcceptSlotsRemaining <= 0 || snapshot.Exhausted)) continue;

            snapshot.Runnable.Add(tribe);
            var needed = Math.Min(tribe.AcceptSlotsRemaining, budget);
            snapshot.AllowancesNeeded += needed;
            budget -= needed;
        }
    }

    private static Dictionary<uint, TribeInfo> BuildIndex()
    {
        var tribes = TribeRegistry.Tribes;
        var index = new Dictionary<uint, TribeInfo>(tribes.Length);
        for (var tribeIndex = 0; tribeIndex < tribes.Length; tribeIndex++) index[tribes[tribeIndex].BeastTribeId] = tribes[tribeIndex];
        return index;
    }
}
