using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Tribes;

namespace AutoDailyTribes.Core.Tasks;

public sealed partial class AutoTribe
{
    private int announcedIssuer = -1;

    private TribeIssuer CurrentIssuer => tribe.Issuers[issuerIndex];

    private string CurrentIssuerName => IssuerResolver.Name(CurrentIssuer.BaseId);

    // Picks the quest giver to walk to: the best-paying one whose nameplate shows a daily on
    // offer, else one that is not loaded yet (walk over and look), else one that has not been
    // talked to at all, so a stale marker can never skip a tribe outright.
    private bool ChooseIssuer()
    {
        var issuers = tribe.Issuers;
        var outOfView = -1;
        var untried = -1;
        for (var index = 0; index < issuers.Length; index++)
        {
            if (issuerFailPasses[index] >= MaxAcceptFailPasses) continue;

            switch (IssuerProbe.Offer(issuers[index].InstanceId))
            {
                case IssuerOffer.Offering:
                    SwitchIssuer(index, "shows a daily on offer");
                    return true;
                case IssuerOffer.Unknown:
                    if (outOfView < 0) outOfView = index;
                    break;
                default:
                    if (untried < 0 && issuerFailPasses[index] == 0) untried = index;
                    break;
            }
        }

        if (outOfView >= 0)
        {
            SwitchIssuer(outOfView, "not in view yet");
            return true;
        }

        if (untried >= 0)
        {
            SwitchIssuer(untried, "shows no marker, asking once anyway");
            return true;
        }

        Warn($"{tribe.Name}: none of the {issuers.Length} quest giver(s) is offering a daily right now; skipping");
        runOutcome = RunOutcome.Skipped;
        runDetail = "no daily on offer";
        return false;
    }

    private void SwitchIssuer(int index, string reason)
    {
        issuerIndex = index;
        if (announcedIssuer == index) return;

        announcedIssuer = index;
        Diag($"{tribe.Name}: heading to {CurrentIssuerName} ({reason})");
    }

    private void RecordAcceptPass(int acceptedBefore)
    {
        arrivedAtIssuer = false;
        if (tribe.AcceptedTodayCount > acceptedBefore)
        {
            issuerFailPasses[issuerIndex] = 0;
            return;
        }

        var confirmedEmpty = IssuerProbe.Offer(CurrentIssuer.InstanceId) == IssuerOffer.NotOffering;
        issuerFailPasses[issuerIndex] += confirmedEmpty ? MaxAcceptFailPasses : 1;
        Diag(confirmedEmpty
            ? $"{tribe.Name}: {CurrentIssuerName} handed out nothing and shows no daily on offer; moving on"
            : $"{tribe.Name}: {CurrentIssuerName} handed out nothing (pass {issuerFailPasses[issuerIndex]}/{MaxAcceptFailPasses})");
    }
}
