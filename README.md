<p align="center">
  <img src="AutoDailyTribes/Images/Icon.png" width="180" alt="Auto Daily Tribes icon" />
</p>

<h1 align="center">Auto Daily Tribes</h1>

<p align="center">
  <a href="https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/XeldarAlz/FFXIV-AutoDailyTribes?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/XeldarAlz/FFXIV-AutoDailyTribes/total?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/actions/workflows/release.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/XeldarAlz/FFXIV-AutoDailyTribes/release.yml?style=flat-square"></a>
  <a href="LICENSE.md"><img alt="License" src="https://img.shields.io/badge/license-AGPL--3.0--or--later-blue?style=flat-square"></a>
</p>

<p align="center">
  <em>Daily allied tribe quests, done for you. Built on Dalamud.</em>
</p>

---

<p align="center">
  <img src="AutoDailyTribes/Images/demo.gif" alt="Auto Daily Tribes demo" />
</p>

## What it does

Lists every Allied Tribe (formerly Beast Tribe) from A Realm Reborn through Dawntrail in one window. Tick the tribes you want, press **Start**, and the plugin teleports to each issuer, accepts the day's quests, plays them out, and turns them in.

## Features

- A single app window with Tribes, Settings, Plugins and About pages, plus a compact header-bar mode.
- A **Today** card with your allowance ring, a one-line plan and the run order as draggable chips.
- Tribe cards behind an expansion picker, opening on the expansion that has something to run.
- Per-tribe progress on every card: daily slots, rank name, reputation bar and run-order number.
- Filter chips for Combat, Crafter, Gatherer, Mixed and **Not maxed**, hiding tribes from the list and the run.
- Locked and under-rank tribes stay visible, dimmed, with tooltips explaining why.
- A live run view with a progress ring, phase stepper, stat tiles, up-next queue and activity log.
- Cancellable mid-run; selection persists across reloads.
- Per-discipline job preference for crafter, gatherer and combat tribes.

## Install

In-game: `/xlsettings` → **Experimental** → paste into **Custom Plugin Repositories**:

```
https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json
```

Tick **Enabled**, click **+**, then **Save and Close**. Open `/xlplugins` → **All Plugins**, search for **Auto Daily Tribes**, and install.

## Commands

| Command | Action |
|---|---|
| `/adt` | Toggle the main window |
| `/dailytribes` | Alias for `/adt` |
| `/adt config` | Open the Settings page |
| `/adt deps` | Open the Plugins page |
| `/adt about` | Open the About page |
| `/adt target` | Log targeted NPC's BaseId (debug helper) |

## More from me

If you liked this plugin, take a look at my other Dalamud work. You might find something else there for you.

→ [XeldarAlz Dalamud Plugins](https://github.com/XeldarAlz/DalamudPlugins)

## License

AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md).
