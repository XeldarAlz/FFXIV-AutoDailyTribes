# AI usage

This doc records how AI tooling is used to build Auto Daily Tribes and what level of involvement that amounts to. Read it before you open a pull request.

Auto Daily Tribes ships from its own repository, so the [official Dalamud plugin repository's AI policy](https://dalamud.dev/plugin-publishing/ai-policy/) does not bind it. This project holds to that policy anyway, because it is the community's standard for what honest AI use looks like in a Dalamud plugin, and because a plugin nobody is forced to audit is exactly the kind that should be able to survive one. Everything below is written to satisfy it.

## Key files

| Path | Role |
| --- | --- |
| AI-USAGE.md | This doc: declared level, pipeline, and what contributors declare |
| CONTRIBUTING.md | The pull request checklist that points here, and the in-game testing bar every change is held to |

## Declared level

Dalamud's policy uses six levels, adapted from [AI-DECLARATION.md](https://ai-declaration.md/): None, Hint, Assist, Pair, Copilot, Auto.

**Auto Daily Tribes declares Copilot.** AI implements while the human plans, reviews, tests, and owns the result. The AI does most of the writing.

That is the honest label and it is deliberately not the lowest one that could be argued. The implementation step inside a task runs autonomously, which reads as Auto if you look only at that step. It is bounded on both ends by a human who decides what gets built and whether it ships, which is what Copilot describes.

## How the work actually happens

Claude Code is the tool, used as a senior harness rather than as an autocomplete:

- **Orchestrated agent pipelines.** Full workflows built for the task: coder agents that implement, reviewer agents that read the diff back, tester agents that exercise it. Multiple agents run per task, fanning out and reporting into one result.
- **Review passes.** Pull request review and code review run through the same tooling, against CONTRIBUTING.md and .editorconfig as the rulebook.
- **Autonomous execution inside human gates.** The pipeline runs unattended once started. It does not decide what to start.

The human gates, in order, none of them skippable:

1. **The spec comes first.** What to build, and the constraints it has to respect, are written before any agent runs.
2. **The diff is read.** Output is reviewed before it lands, not after users find the problem.
3. **It is tested in the game.** On a real client, running at least one tribe's dailies end to end, from the issuer to the turn-in, in the expansion the change touches. A clean `dotnet build` is not a test.
4. **Ownership transfers.** Every merged line is the maintainer's to defend, explain, and fix. "The AI did it" is not an answer to why something is written the way it is.
5. **Feedback is taken on its merits.** AI-assisted work invites sharper review, from users and from any reviewer, and that scrutiny is earned rather than unfair. The answer to a review comment is a fix or a reason, never a defense of the tooling.

## What this does not change

Nothing in this doc lowers a standard. Code produced with AI assistance is held to exactly the bar every other change meets: the CONTRIBUTING.md checklist, the .editorconfig style, one concern per pull request, and `[ADT]` log lines that make every step of the automation loop auditable. A reviewer cannot tell which lines came from where, and that is the point.

The one thing AI use does change is where the verification effort goes. AI gets Dalamud and FFXIVClientStructs APIs wrong often enough that any call into either is suspect until it has run in game. The IPC calls into Questionable, vnavmesh, TextAdvance, Artisan and AutoHook, the helpers listed in `/adt deps`, deserve the same suspicion: a call that compiles says nothing about whether the other plugin still answers it the same way.

## If you contribute

Three rules, no forms:

1. **Declare your level in the pull request description** if you went beyond autocomplete or inline suggestions. Use Dalamud's six level names so the vocabulary here matches theirs. None and Hint need no declaration.
2. **Test your change in game yourself** before you open the PR. This is already item 2 of the CONTRIBUTING.md checklist.
3. **Be able to explain your code.** If you cannot say why something is written the way it is, it is not ready.

Nobody is judged for the level they declare. An undeclared one is the problem.

## Translations

The eight non-English catalogs under AutoDailyTribes/Localization/ are AI-assisted with human review. English is the source: it is the default text of every string in `Core/Localization/L.cs`, and en.json is never loaded when the plugin runs in English. Game data is not translated at all: tribe and job names are read from the game client, so they always match what the player sees in their own client.

This is the approach Dalamud's policy asks for, and the gap is coverage rather than method: catalogs without a native-speaker pass should be treated as placeholders until one lands. Native corrections are welcome from anyone, through the [translation issue form](https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/issues/new?template=translation_report.yml).

## Gotchas

- **Nobody enforces this on the project, which is the whole point.** A custom repository has no review queue and no ban list, so every rule here holds because the project chose it. Standards that only survive enforcement are not standards.
- **The no-attribution git rule is not concealment.** Commits here carry no co-author trailers or generated-with footers, so a commit message holds substantive content only. Disclosure lives in this doc and in pull request descriptions instead. History is public in any case: 50 commits from the project's first two days, 23 and 24 May 2026, still carry trailers, so AI use in this repo is externally evident whatever any doc says.
- **A clean build is not a test.** Item 3 of the human gates exists because AI output compiles far more reliably than it works. Quest dialog and turn-in flows shift between expansions, so a fix proven on a Dawntrail tribe says nothing about an ARR one.

## Related docs

- [CONTRIBUTING.md](CONTRIBUTING.md): the pull request checklist and the in-game testing bar.
- [TRADEMARK.md](TRADEMARK.md) and [NOTICE](NOTICE): what the license covers, the credit every fork owes, and the name and icon the license does not cover.
