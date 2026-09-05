namespace AutoDailyTribes.Windows.Components;

internal static class ListReorder
{
    public static bool Move<T>(IList<T> list, int from, int to)
    {
        if (from < 0 || from >= list.Count || to < 0 || to >= list.Count || from == to) return false;
        var item = list[from];
        list.RemoveAt(from);
        list.Insert(to, item);
        return true;
    }
}
