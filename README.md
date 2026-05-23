<p align="center">
  <img src="AutoDailyTribes/Images/Icon.png" width="220" alt="Auto Daily Tribes icon" />
</p>

<h1 align="center">Auto Daily Tribes</h1>

<p align="center">
  <a href="https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/XeldarAlz/FFXIV-AutoDailyTribes?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/XeldarAlz/FFXIV-AutoDailyTribes/total?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/actions/workflows/release.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/XeldarAlz/FFXIV-AutoDailyTribes/release.yml?style=flat-square"></a>
  <a href="LICENSE.md"><img alt="License" src="https://img.shields.io/github/license/XeldarAlz/FFXIV-AutoDailyTribes?style=flat-square"></a>
</p>

<p align="center">
  <em>Daily allied tribe quests, hands-off. Built on Dalamud.</em>
</p>

---

## What it does

Lists every Allied Tribe (formerly Beast Tribe) from A Realm Reborn through Dawntrail in one window. Tick the tribes you want, press **Run selected**, and the plugin teleports to each issuer, accepts the day's quests, plays them out, and turns them in.

## Features

- One window listing every Allied Tribe from ARR through DT.
- Click cards to select; **Run selected** automates them back-to-back.
- Per-tribe progress: rank, reputation bar, allowance pill.
- Tribe-specific icons; era-grouped sections (newest first).
- Locked tribes greyed out with hover tooltips explaining why.
- Cancellable mid-run; selection persists across reloads.
- Per-discipline job preference for crafter / gatherer tribes.

## Install

In-game, run `/xlsettings` → **Experimental**.

Under **Custom Plugin Repositories**, paste:

```
https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json
```

Tick **Enabled**, click the **+**, then **Save and Close**.

Open `/xlplugins` → **All Plugins**, search for **Auto Daily Tribes**, and install.

## Commands

| Command | Action |
|---|---|
| `/adt` | Toggle the main window |
| `/dailytribes` | Alias for `/adt` |
| `/adt config` | Open settings |
| `/adt deps` | Open dependencies window |
| `/adt about` | Open credits / links |
| `/adt target` | Log targeted NPC's BaseId (debug helper) |

## License

AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md).
