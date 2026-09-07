using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AutoDailyTribes.Core.Game;

internal enum IssuerOffer { Unknown, Offering, NotOffering }

// Quest givers carry the game's quest marker in their nameplate icon id. The 71xxx family covers
// every quest marker; ids ending in 1 or 2 are "quest available" variants, 5 is a turn-in and the
// rest are in-progress markers.
internal static unsafe class IssuerProbe
{
    private const uint QuestMarkerFirst = 71000;
    private const uint QuestMarkerLast = 71999;
    private const uint AvailableVariant = 1;
    private const uint AvailableLockedVariant = 2;

    public static IssuerOffer Offer(ulong instanceId)
    {
        var gameObject = Svc.Objects.SearchById(instanceId);
        if (gameObject == null) return IssuerOffer.Unknown;

        var iconId = ((GameObject*)gameObject.Address)->NamePlateIconId;
        return OffersQuest(iconId) ? IssuerOffer.Offering : IssuerOffer.NotOffering;
    }

    public static bool OffersQuest(uint namePlateIconId)
    {
        if (namePlateIconId < QuestMarkerFirst || namePlateIconId > QuestMarkerLast) return false;

        var variant = namePlateIconId % 10;
        return variant == AvailableVariant || variant == AvailableLockedVariant;
    }
}
