using AutoDailyTribes.Core.Tribes;
using Dalamud.Interface;

namespace AutoDailyTribes.Windows.Components;

internal static class KindIcon
{
    public static FontAwesomeIcon Icon(TribeKind kind) => kind switch
    {
        TribeKind.Combat   => FontAwesomeIcon.Shield,
        TribeKind.Crafter  => FontAwesomeIcon.Hammer,
        TribeKind.Gatherer => FontAwesomeIcon.Leaf,
        _                  => FontAwesomeIcon.Cubes,
    };
}
