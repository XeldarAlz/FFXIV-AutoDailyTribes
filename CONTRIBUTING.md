# Contributing

Thanks for taking an interest. This is a small solo project, but PRs are welcome and I'll review them.

## Quick start

```bash
git clone --recurse-submodules https://github.com/XeldarAlz/FFXIV-AutoDailyTribes.git
cd FFXIV-AutoDailyTribes
dotnet build AutoDailyTribes.sln -c Release
```

You need the .NET 9 SDK. The plugin requires Dalamud at runtime; CI pulls a Dalamud dev build automatically and that's enough to compile. See `.github/workflows/release.yml` if you want to reproduce CI locally.

Load the built plugin via `/xlsettings` → **Experimental** → **Dev Plugin Locations**, pointing at `AutoDailyTribes/bin/x64/Release/AutoDailyTribes/AutoDailyTribes.dll`.

## Project layout

- `AutoDailyTribes/Core/` — tribe data, quest dispatch, scheduling.
- `AutoDailyTribes/Windows/` — ImGui main window, settings, dependencies.
- `AutoDailyTribes/` — plugin entry points, config, command wiring.
- `ECommons/` — submodule, shared Dalamud helpers. Don't patch this directly; upstream it.

Keep logic small and direct. This plugin has one job.

## Before you open a PR

1. `dotnet build -c Release` cleanly.
2. Test in-game against at least one tribe per expansion era you touched. Quest dialog text and turn-in flows shift between expansions, so a fix that works for Dawntrail may not work for ARR.
3. Keep the diff focused. One concern per PR.
4. Match the existing style. No heavy abstractions "for later."
5. If your change affects what a user sees or types (commands, window layout, settings), update the README.

## Good first issues

Check the tracker for anything labeled `good first issue`. Tribe-specific quest bugs are usually the lowest-friction way to help — pick a tribe that's misbehaving, attach a log of what the plugin did vs. what should have happened, and a fix is usually a small change.

## Security

Please don't file public issues for security problems — see [SECURITY.md](SECURITY.md).

## Code of conduct

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Be decent.

## License

By contributing, you agree your contributions are licensed under AGPL-3.0-or-later, the same as the project.
