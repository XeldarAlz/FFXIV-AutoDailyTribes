using ECommons.DalamudServices;
using Newtonsoft.Json.Linq;
using System.IO;

namespace AutoDailyTribes.Core.Localization;

internal sealed class StringCatalog
{
    private const int FirstNonAsciiCodepoint = 0x0080;

    public static readonly StringCatalog Empty = new(new Dictionary<string, string>(0, StringComparer.Ordinal));

    private readonly Dictionary<string, string> entries;

    private StringCatalog(Dictionary<string, string> entries)
    {
        this.entries = entries;
    }

    public int Count => entries.Count;

    public bool TryGet(string key, out string value) => entries.TryGetValue(key, out value!);

    public bool Contains(string key) => entries.ContainsKey(key);

    public static StringCatalog Load(string path)
    {
        if (!File.Exists(path)) return Empty;

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var root = JObject.Parse(File.ReadAllText(path));
            Flatten(root, null, entries);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, $"{AdtConstants.LogPrefix} Failed to load language catalog '{path}'");
            return Empty;
        }

        return new StringCatalog(entries);
    }

    // ImGui glyph ranges (start/end pairs, zero-terminated) for every non-ASCII codepoint used in the file,
    // so the font atlas bakes exactly the ideographs and letters this language needs.
    public static ushort[] ScanGlyphRanges(string path)
    {
        if (!File.Exists(path)) return [0];

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, $"{AdtConstants.LogPrefix} Failed to scan glyphs from '{path}'");
            return [0];
        }

        var present = new bool[char.MaxValue + 1];
        for (var index = 0; index < text.Length; index++)
        {
            var codepoint = text[index];
            if (codepoint < FirstNonAsciiCodepoint || char.IsSurrogate(codepoint)) continue;
            present[codepoint] = true;
        }

        var ranges = new List<ushort>();
        var runStart = -1;
        for (var codepoint = FirstNonAsciiCodepoint; codepoint <= char.MaxValue; codepoint++)
        {
            if (present[codepoint])
            {
                if (runStart < 0) runStart = codepoint;
                continue;
            }

            if (runStart < 0) continue;
            ranges.Add((ushort)runStart);
            ranges.Add((ushort)(codepoint - 1));
            runStart = -1;
        }

        if (runStart >= 0)
        {
            ranges.Add((ushort)runStart);
            ranges.Add(char.MaxValue);
        }

        ranges.Add(0);
        return [.. ranges];
    }

    private static void Flatten(JObject node, string? prefix, Dictionary<string, string> target)
    {
        foreach (var property in node.Properties())
        {
            var key = prefix is null ? property.Name : string.Concat(prefix, ".", property.Name);
            if (property.Value is JObject child)
            {
                Flatten(child, key, target);
                continue;
            }

            target[key] = property.Value.ToString();
        }
    }
}
