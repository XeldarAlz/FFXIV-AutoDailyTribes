namespace AutoDailyTribes.Core.Localization;

internal static class L
{
    internal static class Common
    {
        public static readonly LocString Close = new("common.close", "Close");
        public static readonly LocString Clear = new("common.clear", "Clear");
        public static readonly LocString Search = new("common.search", "Search…");
        public static readonly LocString NoMatches = new("common.noMatches", "No matches for \"{0}\"");
        public static readonly LocString Working = new("common.working", "Working…");
        public static readonly LocString OpenCopyHint = new("common.openCopyHint", "Click to open · right-click to copy");
        public static readonly LocPlural TribesCount = new("common.tribesCount", "{0} tribe", "{0} tribes");
    }

    internal static class Kind
    {
        public static readonly LocString Combat = new("kind.combat", "Combat");
        public static readonly LocString Crafter = new("kind.crafter", "Crafter");
        public static readonly LocString Gatherer = new("kind.gatherer", "Gatherer");
        public static readonly LocString Mixed = new("kind.mixed", "Mixed");
    }

    internal static class Era
    {
        public static readonly LocString Arr = new("era.arr", "A Realm Reborn");
        public static readonly LocString Hw = new("era.hw", "Heavensward");
        public static readonly LocString Sb = new("era.sb", "Stormblood");
        public static readonly LocString Shb = new("era.shb", "Shadowbringers");
        public static readonly LocString Ew = new("era.ew", "Endwalker");
        public static readonly LocString Dt = new("era.dt", "Dawntrail");
    }

    internal static class Rank
    {
        public static readonly LocString Neutral = new("rank.neutral", "Neutral");
        public static readonly LocString Recognized = new("rank.recognized", "Recognized");
        public static readonly LocString Friendly = new("rank.friendly", "Friendly");
        public static readonly LocString Trusted = new("rank.trusted", "Trusted");
        public static readonly LocString Respected = new("rank.respected", "Respected");
        public static readonly LocString Honored = new("rank.honored", "Honored");
        public static readonly LocString Sworn = new("rank.sworn", "Sworn");
        public static readonly LocString Bloodsworn = new("rank.bloodsworn", "Bloodsworn");
        public static readonly LocString Allied = new("rank.allied", "Allied");
        public static readonly LocString Locked = new("rank.locked", "Locked");
        public static readonly LocString Numbered = new("rank.numbered", "Rank {0}");
        public static readonly LocString NamedLabel = new("rank.namedLabel", "Rank {0} · {1}");
    }

    internal static class Shell
    {
        public static readonly LocString NavTribes = new("shell.nav.tribes", "Tribes");
        public static readonly LocString NavSettings = new("shell.nav.settings", "Settings");
        public static readonly LocString NavPlugins = new("shell.nav.plugins", "Plugins");
        public static readonly LocString NavAbout = new("shell.nav.about", "About");
        public static readonly LocString StatusRunning = new("shell.status.running", "Running");
        public static readonly LocString StatusReady = new("shell.status.ready", "Ready");
        public static readonly LocString StatusPickTribes = new("shell.status.pickTribes", "Pick tribes");
        public static readonly LocString StatusSetupNeeded = new("shell.status.setupNeeded", "Setup needed");
        public static readonly LocString StatusAllDone = new("shell.status.allDone", "All done");
        public static readonly LocString Minimize = new("shell.minimize", "Minimize to the header bar");
        public static readonly LocString Restore = new("shell.restore", "Restore the window");
        public static readonly LocString CompactPick = new("shell.compactPick", "Pick tribes to begin");
        public static readonly LocString CompactPlan = new("shell.compactPlan", "{0} · {1} allowances · reset in {2}");
        public static readonly LocString CompactRunning = new("shell.compactRunning", "{0}  ·  {1}");
        public static readonly LocString Start = new("shell.start", "Start");
        public static readonly LocString Stop = new("shell.stop", "Stop");
        public static readonly LocString StartSub = new("shell.startSub", "{0} · {1} allowances · reset in {2}");
        public static readonly LocString StopSub = new("shell.stopSub", "Tribe {0} of {1} · {2}");
        public static readonly LocString ReasonInstall = new("shell.reason.install", "Install the required plugins first");
        public static readonly LocString ReasonPick = new("shell.reason.pick", "Pick a tribe below");
        public static readonly LocString ReasonExhausted = new("shell.reason.exhausted", "All allowances spent, back after the reset");
        public static readonly LocString ReasonDone = new("shell.reason.done", "Your tribes are done for today");
        public static readonly LocString StopHint = new("shell.stopHint", "Stop the run");
        public static readonly LocString Waiting = new("shell.waiting", "Waiting for the next tribe…");
        public static readonly LocString MiniStatus = new("shell.miniStatus", "{0}   ·   {1}");
    }

    internal static class Tribes
    {
        public static readonly LocString GreetingMorning = new("tribes.greeting.morning", "Good morning, ready for your dailies?");
        public static readonly LocString GreetingAfternoon = new("tribes.greeting.afternoon", "Good afternoon, ready for your dailies?");
        public static readonly LocString GreetingEvening = new("tribes.greeting.evening", "Good evening, ready for your dailies?");
        public static readonly LocString GreetingNight = new("tribes.greeting.night", "Late night, still on dailies?");

        public static readonly LocString TitleRunning = new("tribes.title.running", "Running your dailies.");
        public static readonly LocString TitleSetup = new("tribes.title.setup", "Install the required plugins first.");
        public static readonly LocString DetailSetup = new("tribes.detail.setup", "vnavmesh, Questionable and TextAdvance do the walking, questing and dialogue.");
        public static readonly LocString TitleExhausted = new("tribes.title.exhausted", "All allowances spent for today.");
        public static readonly LocString DetailExhausted = new("tribes.detail.exhausted", "Fresh allowances arrive with the reset in {0}.");
        public static readonly LocString TitlePick = new("tribes.title.pick", "Pick the tribes to run.");
        public static readonly LocString DetailPick = new("tribes.detail.pick", "Tap a card below. Your list is remembered, so tomorrow is one click.");
        public static readonly LocString TitleDone = new("tribes.title.done", "Your tribes are done for today.");
        public static readonly LocString DetailDone = new("tribes.detail.done", "The list stays and runs again after the reset in {0}.");
        public static readonly LocString TitleReady = new("tribes.title.ready", "Ready to run {0}.");
        public static readonly LocString DetailReadySkipped = new("tribes.detail.readySkipped", "{0} of your {1} picks are finished, hidden or locked and will be skipped.");
        public static readonly LocString DetailReady = new("tribes.detail.ready", "Uses {0} of the {1} allowances left today.");
        public static readonly LocString OpenPlugins = new("tribes.openPlugins", "Open Plugins");
        public static readonly LocString ResetIn = new("tribes.resetIn", "Reset in {0}");
        public static readonly LocString ResetAt = new("tribes.resetAt", "Daily reset at {0}");

        public static readonly LocString Today = new("tribes.today", "Today");
        public static readonly LocString ClearHint = new("tribes.clearHint", "Empties your standing pick, including tribes hidden by the filters.");
        public static readonly LocString RingCaption = new("tribes.ringCaption", "left");
        public static readonly LocString RingTooltip = new("tribes.ringTooltip", "{0} of {1} daily allowances used. Every tribe offers {2} a day, so a full day is {3} tribes. The game spends an allowance the moment a quest is accepted, not when it is turned in.");
        public static readonly LocString SummaryEmpty = new("tribes.summary.empty", "Nothing picked yet · {0} of {1} allowances left · reset in {2}");
        public static readonly LocString SummaryPicked = new("tribes.summary.picked", "{0} picked · {1} runnable now · needs {2} of {3} allowances · reset in {4}");

        public static readonly LocString StripEmpty = new("tribes.strip.empty", "Pick tribes below. They run in the order you add them, and the list is remembered for tomorrow.");
        public static readonly LocString StripDrag = new("tribes.strip.drag", "Drag to reorder");
        public static readonly LocString StripRemove = new("tribes.strip.remove", "Remove from the run");
        public static readonly LocString StripDone = new("tribes.strip.done", "done");
        public static readonly LocString StripLocked = new("tribes.strip.locked", "locked");
        public static readonly LocString StripHidden = new("tribes.strip.hidden", "hidden");
        public static readonly LocString StripRank = new("tribes.strip.rank", "rank {0}");

        public static readonly LocString Library = new("tribes.library", "Tribes");
        public static readonly LocString NotMaxed = new("tribes.notMaxed", "Not maxed");
        public static readonly LocString NotMaxedOn = new("tribes.notMaxedOn", "Showing only tribes below max rank. Click to bring maxed tribes back into the list and the run.");
        public static readonly LocString NotMaxedOff = new("tribes.notMaxedOff", "Hide tribes already at max rank from the list and the run.");
        public static readonly LocString KindHide = new("tribes.kindHide", "Hide {0} tribes from the list and the run.");
        public static readonly LocString KindShow = new("tribes.kindShow", "Show {0} tribes again.");
        public static readonly LocString EraSummary = new("tribes.eraSummary", "{0} of {1} unlocked · {2} ready · {3} in your list");
        public static readonly LocString EmptyFiltered = new("tribes.emptyFiltered", "Every tribe here is hidden by the filters above.");

        public static readonly LocString CardDailies = new("tribes.card.dailies", "Dailies");
        public static readonly LocString CardRank = new("tribes.card.rank", "Rank");
        public static readonly LocString CardRepLocked = new("tribes.card.repLocked", "–");
        public static readonly LocString CardRepMaxed = new("tribes.card.repMaxed", "MAX");
        public static readonly LocString CardDone = new("tribes.card.done", "Done today");
        public static readonly LocString CardLocked = new("tribes.card.locked", "Locked");
        public static readonly LocString CardRankNeeded = new("tribes.card.rankNeeded", "Rank {0} needed");

        public static readonly LocString TipUnlock = new("tribes.tip.unlock", "Complete the intro quest in game to unlock this tribe.");
        public static readonly LocString TipReachRank = new("tribes.tip.reachRank", "Reach rank {0} to run dailies.");
        public static readonly LocString TipSlots = new("tribes.tip.slots", "{0} / {1} daily slots used · {2} allowances shared across all tribes.");
        public static readonly LocString TipAllUsed = new("tribes.tip.allUsed", "All daily slots used for this tribe today.");
        public static readonly LocString TipCanRankUp = new("tribes.tip.canRankUp", "Daily rep is full. Finish the rank-up quest in game to refresh 3 more dailies today.");
        public static readonly LocString TipKeepSelected = new("tribes.tip.keepSelected", "Still in your list, so it runs again after the reset. Click to drop it.");
        public static readonly LocString TipKeepUnselected = new("tribes.tip.keepUnselected", "Click to keep it in your list for after the reset.");
        public static readonly LocString TipInJournal = new("tribes.tip.inJournal", "{0} accepted quest(s) still in the journal. Click to run them.");
        public static readonly LocString TipRemove = new("tribes.tip.remove", "In your list. Click to remove it from the run.");
        public static readonly LocString TipAdd = new("tribes.tip.add", "Click to add it to the run.");
        public static readonly LocString TipRefreshed = new("tribes.tip.refreshed", "Ranked up today, so 3 fresh dailies are available.");
        public static readonly LocString TipGatherer = new("tribes.tip.gatherer", "Gathering dailies bind to the class you accept them with.");
    }

    internal static class Run
    {
        public static readonly LocString PhaseSwitchingJob = new("run.phase.switchingJob", "Switching job");
        public static readonly LocString PhaseTraveling = new("run.phase.traveling", "Traveling");
        public static readonly LocString PhaseAccepting = new("run.phase.accepting", "Accepting dailies");
        public static readonly LocString PhaseDelegating = new("run.phase.delegating", "Running quests");
        public static readonly LocString PhaseRecovering = new("run.phase.recovering", "Recovering");
        public static readonly LocString PhaseDone = new("run.phase.done", "Tribe complete");
        public static readonly LocString PhasePreparing = new("run.phase.preparing", "Preparing");
        public static readonly LocString PhaseStandingBy = new("run.phase.standingBy", "Standing by");

        public static readonly LocString HeaderFooter = new("run.headerFooter", "Tribe {0} of {1} · {2}");
        public static readonly LocString RingCaption = new("run.ringCaption", "tribes");

        public static readonly LocString StepSwitchJob = new("run.step.switchJob", "Switch job");
        public static readonly LocString StepTravel = new("run.step.travel", "Travel");
        public static readonly LocString StepAccept = new("run.step.accept", "Accept");
        public static readonly LocString StepRunQuests = new("run.step.runQuests", "Run quests");
        public static readonly LocString StepLeft = new("run.step.left", "{0} left");

        public static readonly LocString TileAllowances = new("run.tile.allowances", "Allowances");
        public static readonly LocString TileCurrent = new("run.tile.current", "This tribe");
        public static readonly LocString TileElapsed = new("run.tile.elapsed", "Elapsed");
        public static readonly LocString TileReset = new("run.tile.reset", "Reset in");
        public static readonly LocString TileLeft = new("run.tile.left", "{0} left");
        public static readonly LocString TileInJournal = new("run.tile.inJournal", "{0} in journal");

        public static readonly LocString UpNext = new("run.upNext", "Up next");
        public static readonly LocString Activity = new("run.activity", "Activity");
        public static readonly LocString QueueMeta = new("run.queueMeta", "{0} dailies · {1}");
    }

    internal static class Settings
    {
        public static readonly LocString Title = new("settings.title", "Settings");
        public static readonly LocString CatGeneral = new("settings.cat.general", "General");
        public static readonly LocString CatGeneralSub = new("settings.cat.generalSub", "How the window behaves and how the tribe list is arranged.");
        public static readonly LocString CatJobs = new("settings.cat.jobs", "Jobs");
        public static readonly LocString CatJobsSub = new("settings.cat.jobsSub", "Which job runs each kind of tribe.");
        public static readonly LocString CatAfterRun = new("settings.cat.afterRun", "After the run");
        public static readonly LocString CatAfterRunSub = new("settings.cat.afterRunSub", "Chat commands to fire once every queued tribe has finished.");

        public static readonly LocString Language = new("settings.language", "Language");
        public static readonly LocString LanguageHelp = new("settings.languageHelp", "Language for this plugin's interface. Tribe names always follow your game client.");

        public static readonly LocString GroupWindow = new("settings.group.window", "Window");
        public static readonly LocString OpenOnLogin = new("settings.openOnLogin", "Open on login when dailies are available");
        public static readonly LocString OpenOnLoginHelp = new("settings.openOnLoginHelp", "Shows this window after logging in whenever you still have daily allowances to spend.");

        public static readonly LocString GroupList = new("settings.group.list", "Tribe list");
        public static readonly LocString ExpansionOrder = new("settings.expansionOrder", "Expansion order");
        public static readonly LocString ExpansionOrderHelp = new("settings.expansionOrderHelp", "Which end of the expansion picker the newest content sits on.");
        public static readonly LocString OrderNewest = new("settings.order.newest", "Newest first");
        public static readonly LocString OrderNewestDetail = new("settings.order.newestDetail", "Dawntrail leads the expansion picker, A Realm Reborn closes it.");
        public static readonly LocString OrderOldest = new("settings.order.oldest", "Oldest first");
        public static readonly LocString OrderOldestDetail = new("settings.order.oldestDetail", "A Realm Reborn leads the expansion picker, Dawntrail closes it.");

        public static readonly LocString JobRow = new("settings.jobRow", "Job to use");
        public static readonly LocString JobSpecificRow = new("settings.jobSpecificRow", "Which job");
        public static readonly LocString JobSpecificHelp = new("settings.jobSpecificHelp", "The gearset for this job is equipped before the tribe's dailies are accepted.");
        public static readonly LocString JobCurrent = new("settings.job.current", "Currently equipped");
        public static readonly LocString JobCurrentDetail = new("settings.job.currentDetail", "Keeps whatever job of this type you are wearing when the run starts.");
        public static readonly LocString JobHighest = new("settings.job.highest", "Highest level");
        public static readonly LocString JobHighestDetail = new("settings.job.highestDetail", "Switches to the gearset of your highest-level job of this type.");
        public static readonly LocString JobLowest = new("settings.job.lowest", "Lowest level");
        public static readonly LocString JobLowestDetail = new("settings.job.lowestDetail", "Switches to the gearset of your lowest-level job of this type, handy for levelling.");
        public static readonly LocString JobSpecific = new("settings.job.specific", "Specific job");
        public static readonly LocString JobSpecificDetail = new("settings.job.specificDetail", "Always switches to the job picked below.");

        // The scope lines list tribes by their in-game proper nouns, so translators leave them as they are.
        public static readonly LocString CrafterTitle = new("settings.crafter.title", "Crafter tribes");
        public static readonly LocString CrafterHelp = new("settings.crafter.help", "Which Disciple of the Hand job runs crafter tribe dailies.");
        public static readonly LocString CrafterScope = new("settings.crafter.scope", "Ixal · Moogles · Dwarves · Loporrits · Yok Huy");
        public static readonly LocString GathererTitle = new("settings.gatherer.title", "Gatherer tribes");
        public static readonly LocString GathererHelp = new("settings.gatherer.help", "Which Disciple of the Land job runs gatherer tribe dailies.");
        public static readonly LocString GathererScope = new("settings.gatherer.scope", "Qitari · Omicron · Mamool Ja");
        public static readonly LocString GathererFootnote = new("settings.gatherer.footnote", "Fisher is excluded. Questionable cannot automate fishing, so gatherer tribes run on Miner or Botanist and any fishing daily is skipped.");
        public static readonly LocString CombatTitle = new("settings.combat.title", "Combat tribes");
        public static readonly LocString CombatHelp = new("settings.combat.help", "Which combat job runs battle tribe dailies.");
        public static readonly LocString CombatScope = new("settings.combat.scope", "Amalj'aa · Sylphs · Kobolds · Sahagin · Vanu Vanu · Vath · Kojin · Ananta · Pixies · Arkasodara · Pelupelu");

        public static readonly LocString PostRunGroup = new("settings.postRun.group", "Chat commands");
        public static readonly LocString PostRunLabel = new("settings.postRun.label", "Run when the batch finishes");
        public static readonly LocString PostRunHelp = new("settings.postRun.help", "One command per line, each starting with a slash. They fire only when every queued tribe has finished on its own, never after Stop.");
        public static readonly LocString PostRunCaption = new("settings.postRun.caption", "Lines that do not start with a slash are skipped, so nothing is ever said in chat. Try /li home to head home or /ays m to hand off to AutoRetainer.");
    }

    internal static class Plugins
    {
        public static readonly LocString Title = new("plugins.title", "Plugins");
        public static readonly LocString AllInstalled = new("plugins.allInstalled", "All required plugins are installed and loaded.");
        public static readonly LocPlural Missing = new("plugins.missing", "{0} required plugin is missing.", "{0} required plugins are missing.");
        public static readonly LocString Required = new("plugins.required", "Required");
        public static readonly LocString Optional = new("plugins.optional", "Optional");
        public static readonly LocString Installed = new("plugins.installed", "Installed");
        public static readonly LocString Disabled = new("plugins.disabled", "Disabled");
        public static readonly LocString Install = new("plugins.install", "Install");
        public static readonly LocString Installing = new("plugins.installing", "Installing…");
        public static readonly LocString Footer = new("plugins.footer", "Install adds the plugin's repository to Dalamud and queues an install. If one-click install fails, click a plugin name to open its repository or right-click to copy the URL, then add it under /xlsettings, Experimental, Custom Plugin Repositories.");
        public static readonly LocString TextAdvanceDisabled = new("plugins.textAdvanceDisabled", "Loaded, but TextAdvance's own \"Enable plugin\" toggle is off. Questionable needs it on to advance quest dialogue and cutscenes; with it off, dailies stall mid-quest. Turn it on in TextAdvance's settings.");
        public static readonly LocString RepoHint = new("plugins.repoHint", "{0}\nClick to open · right-click to copy");
        public static readonly LocString PurposeVnavmesh = new("plugins.purpose.vnavmesh", "Pathfinding and walking to NPCs.");
        public static readonly LocString PurposeQuestionable = new("plugins.purpose.questionable", "Plays out each daily quest after the plugin accepts it.");
        public static readonly LocString PurposeTextAdvance = new("plugins.purpose.textAdvance", "Auto-advances quest dialogue and cutscenes so Questionable can complete each daily.");
        public static readonly LocString PurposeArtisan = new("plugins.purpose.artisan", "Crafter tribes, invoked by Questionable's internal pipeline.");
    }

    internal static class About
    {
        public static readonly LocString Connect = new("about.connect", "Connect");
        public static readonly LocString Version = new("about.version", "v {0}");
        public static readonly LocString MadeBy = new("about.madeBy", "Made by {0}");
        public static readonly LocString SupportTitle = new("about.supportTitle", "Made with care");
        public static readonly LocString SupportBody = new("about.supportBody", "I build and maintain this in my spare time. If it has helped you, a Patreon membership lets me keep improving it. No pressure, and thank you for being here.");
        public static readonly LocString SupportButton = new("about.supportButton", "Support on Patreon");
        public static readonly LocString PatreonHint = new("about.patreonHint", "Open Patreon · right-click to copy");
        public static readonly LocString LinkGitHub = new("about.link.github", "GitHub");
        public static readonly LocString LinkDiscord = new("about.link.discord", "Discord");
        public static readonly LocString LinkDiscussions = new("about.link.discussions", "Discussions");
        public static readonly LocString LinkBug = new("about.link.bug", "Report a bug");
        public static readonly LocString LinkMore = new("about.link.more", "More plugins");
        public static readonly LocString LinkSecurity = new("about.link.security", "Security");

        public static readonly LocString ReminderTitle = new("about.reminderTitle", "A little reminder");
        public static readonly LocString[] Reminders =
        [
            new("about.reminder.1", "Been at it a while? Roll your shoulders and take one slow breath."),
            new("about.reminder.2", "Hydration check. When did you last drink some water?"),
            new("about.reminder.3", "Blink a few times and let your eyes rest for a moment."),
            new("about.reminder.4", "Stand up, stretch, and shake out your hands. Future you says thanks."),
            new("about.reminder.5", "Sit up and settle in comfortably. Your back will thank you later."),
            new("about.reminder.6", "Remember to eat something today. You matter more than any score."),
            new("about.reminder.7", "Eyes feel tired? Look at something far away for twenty seconds."),
            new("about.reminder.8", "Whatever you're chasing, you're allowed to take a break whenever."),
            new("about.reminder.9", "You're doing great. Be a little kinder to yourself today."),
            new("about.reminder.10", "A glass of water and a quick stretch can reset a long session."),
            new("about.reminder.11", "Unclench your jaw and drop your shoulders. There you go."),
            new("about.reminder.12", "Rest is part of the journey too. Step away whenever you need to."),
        ];

        public static readonly LocString FactsTitle = new("about.factsTitle", "Did you know?");
        public static readonly LocString[] Facts =
        [
            new("about.fact.1", "Honey never spoils. Jars over 3,000 years old have been found still edible."),
            new("about.fact.2", "Octopuses have three hearts and blue blood."),
            new("about.fact.3", "A day on Venus is longer than a whole year on Venus."),
            new("about.fact.4", "Bananas are berries, but strawberries aren't."),
            new("about.fact.5", "There are more possible chess games than atoms in the observable universe."),
            new("about.fact.6", "Sharks have been around longer than trees have."),
            new("about.fact.7", "A group of flamingos is called a flamboyance."),
            new("about.fact.8", "Honeybees can recognize individual human faces."),
            new("about.fact.9", "Wombat droppings are cube shaped."),
            new("about.fact.10", "The Eiffel Tower can grow over 15 cm taller on a hot day."),
            new("about.fact.11", "Hot water can sometimes freeze faster than cold water."),
            new("about.fact.12", "A bolt of lightning is roughly five times hotter than the surface of the Sun."),
        ];

        public static readonly LocString QuotesTitle = new("about.quotesTitle", "Words to live by");
        public static readonly LocString[] Quotes =
        [
            new("about.quote.1", "Done is better than perfect. You can always polish later."),
            new("about.quote.2", "Small steps every day add up to surprising distances."),
            new("about.quote.3", "Comparison is the thief of joy. Run your own race."),
            new("about.quote.4", "Progress, not perfection."),
            new("about.quote.5", "You don't have to be great to start, but you have to start to be great."),
            new("about.quote.6", "Be patient with yourself. Growth takes time."),
            new("about.quote.7", "The best time to begin was yesterday. The second best is right now."),
            new("about.quote.8", "Celebrate the small wins. They count too."),
            new("about.quote.9", "Slow progress is still progress."),
            new("about.quote.10", "Your only real competition is who you were yesterday."),
        ];

        public static readonly LocString JokesTitle = new("about.jokesTitle", "Just for fun");
        public static readonly LocString[] Jokes =
        [
            new("about.joke.1", "Why don't scientists trust atoms? Because they make up everything."),
            new("about.joke.2", "I would tell you a chemistry joke, but I know I wouldn't get a reaction."),
            new("about.joke.3", "Why did the scarecrow win an award? He was outstanding in his field."),
            new("about.joke.4", "I'm reading a book about anti-gravity. It's impossible to put down."),
            new("about.joke.5", "Why don't skeletons fight each other? They don't have the guts."),
            new("about.joke.6", "What do you call fake spaghetti? An impasta."),
            new("about.joke.7", "Why did the bicycle fall over? It was two tired."),
            new("about.joke.8", "What do you call cheese that isn't yours? Nacho cheese."),
            new("about.joke.9", "I'm on a seafood diet. I see food, and I eat it."),
            new("about.joke.10", "I only know 25 letters of the alphabet. I don't know y."),
        ];
    }

    internal static class Plugin
    {
        public static readonly LocString CommandHelp = new("plugin.commandHelp", "Toggle the Auto Daily Tribes window. /adt config | deps | about | target (dump current target's BaseId).");
        public static readonly LocString CommandHelpAlias = new("plugin.commandHelpAlias", "Alias for /adt.");
    }
}
