<p align="center">
  <img src="AutoTribeQuests/Images/Icon.png" width="220" alt="Allied Tribes icon" />
</p>

<h1 align="center">Allied Tribes</h1>

<p align="center">
  <a href="https://github.com/XeldarAlz/FFXIV-AutoTribeQuests/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/XeldarAlz/FFXIV-AutoTribeQuests?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoTribeQuests/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/XeldarAlz/FFXIV-AutoTribeQuests/total?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoTribeQuests/actions/workflows/release.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/XeldarAlz/FFXIV-AutoTribeQuests/release.yml?style=flat-square"></a>
  <a href="LICENSE.md"><img alt="License" src="https://img.shields.io/github/license/XeldarAlz/FFXIV-AutoTribeQuests?style=flat-square"></a>
  <img alt="Beast Tribes" src="https://img.shields.io/badge/Tribes-ARR%20%E2%86%92%20DT-4FC3D1?style=flat-square">
</p>

<p align="center">
  <em>Daily allied tribe quests, hands-off. Built on Dalamud.</em>
</p>

---

## What it does

Lists every Allied Tribe (formerly Beast Tribe) from A Realm Reborn through Dawntrail. Press **Do dailies** on a row and the plugin:

1. Teleports you to that tribe's daily issuer NPC.
2. Accepts the available daily quests (up to 3, respecting the 12-per-day global cap).
3. Hands them off to **Questionable**, which plays them through to objective complete — combat, gathering, crafting, escorts, all of it.
4. Returns to the issuer and turns them in.

Or press **Run all unlocked** in the header and walk away.

## Features

- **One window, every tribe.** Era-grouped cards (ARR → DT), each showing rank, reputation progress, today's allowance, and a single action button.
- **Per-tribe daily automation.** Click one, walk away.
- **Run-all queue.** Sequentially runs every unlocked tribe with room left in your daily allowance.
- **Crafter-tribe friendly.** Ixal / Moogles / Dwarves / Loporrits work through Artisan via Questionable's internal pipeline — no extra setup.
- **Reputation + allowance display.** Rank, current rep, slots taken today, and the 12-quest global cap surfaced in the UI.
- **Locked tribes greyed out.** Intro quest not done? Rank too low? The button is disabled with a hover tooltip explaining why.
- **Cancellable.** **Stop** in the header aborts the current task immediately.
- **Composable UI.** Theme tokens live in `Windows/Styling.cs`; components are split into `Windows/Components/` and `Windows/Sections/` so the window is easy to extend.

## Requirements

| Plugin | Why | Required |
|---|---|---|
| [vnavmesh](https://github.com/awgil/ffxiv_navmesh) | Pathfinding and walking to the issuer | yes |
| [Questionable](https://github.com/WigglyMuffin/Questionable) | Actually plays the quests | yes |
| [Artisan](https://github.com/PunishXIV/Artisan) | Crafter-tribe quests (called via Questionable) | optional |

## Install

Allied Tribes is distributed through XeldarAlz's custom Dalamud plugin repository — one URL, all plugins.

1. In-game, run `/xlsettings` → **Experimental**.
2. Under **Custom Plugin Repositories**, paste:
   ```
   https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json
   ```
   Tick **Enabled**, click the **+**, then **Save and Close**.
3. Open `/xlplugins` → **All Plugins**, search for **Allied Tribes**, and install.

Updates are delivered automatically whenever a new release is cut. Any future plugins published under [@XeldarAlz](https://github.com/XeldarAlz) also appear here without needing a new URL.

## Commands

| Command | Action |
|---|---|
| `/atq` | Toggle the main window |
| `/tribequests` | Alias for `/atq` |
| `/atq config` | Open settings |
| `/atq about` | Open credits / links |

## Configuration

Open via `/atq config` or the gear icon in the main window's toolbar.

**Behavior**
- Auto-open window when dailies are available.
- Stop when the 12-per-day allowance cap is reached.
- Debug UI toggle.

**Tribes**
- Per-tribe enable / disable. Disabled tribes are greyed out and skipped by **Run all unlocked**.

**Crafter tribes**
- DoH job selection (specific job, current job, highest XP, lowest XP). Used when accepting crafter dailies so Questionable picks up the right recipe path.

## Tribe coverage

**Legend:** ✅ wired with verified IDs · 🚧 stub IDs, needs verification · ❔ data not yet entered

### A Realm Reborn
| Tribe | Status | Notes |
|---|---|---|
| Amalj'aa | 🚧 | Combat — Eastern Thanalan |
| Sylphs | 🚧 | Combat — East Shroud |
| Kobolds | 🚧 | Combat — Outer La Noscea |
| Sahagin | 🚧 | Combat — Western La Noscea |
| Ixal | 🚧 | Crafter — North Shroud |

### Heavensward
| Tribe | Status | Notes |
|---|---|---|
| Vanu Vanu | ❔ | Combat — Sea of Clouds |
| Vath | ❔ | Combat — The Dravanian Forelands |
| Moogles | ❔ | Crafter — The Churning Mists |

### Stormblood
| Tribe | Status | Notes |
|---|---|---|
| Kojin | ❔ | Combat — Ruby Sea (SelectString hop on entry) |
| Ananta | ❔ | Combat — The Fringes |
| Namazu | ❔ | Mixed — Yanxia (clan selector quirk) |

### Shadowbringers
| Tribe | Status | Notes |
|---|---|---|
| Pixies | ❔ | Combat — Il Mheg |
| Qitari | ❔ | Gatherer (DoL) — The Rak'tika Greatwood |
| Dwarves | ❔ | Crafter (DoH) — Kholusia |

### Endwalker
| Tribe | Status | Notes |
|---|---|---|
| Arkasodara | ❔ | Combat — Thavnair |
| Omicron | ❔ | Gatherer (DoL) — Ultima Thule |
| Loporrits | ❔ | Crafter (DoH) — Mare Lamentorum |

### Dawntrail
| Tribe | Status | Notes |
|---|---|---|
| Pelupelu | ❔ | Combat (7.1) — Kozama'uka, Dock Poga |
| Mamool Ja | ❔ | Gatherer / DoL (7.2) — Yak T'el, Gok Golma |
| Yok Huy | ❔ | Crafter / DoH (7.35) — Urqopacha, Worlar's Echo |

## License

AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md).
