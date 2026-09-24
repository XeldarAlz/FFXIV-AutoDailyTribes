using AutoDailyTribes.Core.Localization;

namespace AutoDailyTribes.Core.Changelog;

internal readonly record struct ChangelogEntry(string Version, string Date, LocString[] Highlights);
