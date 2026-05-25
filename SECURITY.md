# Security policy

## Supported versions

Only the latest release published to the custom Dalamud repo is supported. If you're on an older `vX.Y.Z`, please update before reporting.

## Reporting a vulnerability

Please report security issues privately via GitHub's private vulnerability reporting:

https://github.com/XeldarAlz/FFXIV-AutoDailyTribes/security/advisories/new

Please don't open a public issue or Discussion for anything that could let someone else exploit users of the plugin before a fix is out.

What counts:

- Code execution or crashes triggerable by crafted game state.
- The plugin teleporting, accepting quests, or persisting something it shouldn't (beyond the documented daily-tribe automation, which is the feature).
- Data exfiltration: this plugin makes no network calls by design; any network traffic would be a bug.

What doesn't:

- The fact that Auto Daily Tribes runs the daily tribe quest loop for you. That's against Square Enix's Terms of Service and is the whole feature; turning it on is the user's choice.

I'll aim to acknowledge reports within a few days and to ship a fix or workaround as soon as I've verified the issue.
