using System.IO;

namespace PixelHedgies;

internal static class AnimalSkinSelection
{
    internal const string Hedgehog = "Hedgehog";
    internal const string Random = "Random";

    internal static IReadOnlyList<string> Discover(string imageDirectory)
    {
        var skins = new List<string> { Hedgehog };
        if (!Directory.Exists(imageDirectory)) return skins;

        foreach (var path in Directory.EnumerateFiles(imageDirectory, "*.png")
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var name = SkinNameFromFile(path);
            if (name is null) continue;
            if (name.Equals(Random, StringComparison.OrdinalIgnoreCase) ||
                skins.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            skins.Add(name);
        }
        return skins;
    }

    internal static string? SkinNameFromFile(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var frameMarker = name.LastIndexOf("-frame-", StringComparison.OrdinalIgnoreCase);
        if (frameMarker < 0 || !int.TryParse(name[(frameMarker + 7)..], out var frame)) return name;
        return frame == 0 ? name[..frameMarker] : null;
    }

    internal static string Resolve(string selection, IReadOnlyList<string> skins, int randomIndex) =>
        selection.Equals(Random, StringComparison.OrdinalIgnoreCase)
            ? skins[(int)((uint)randomIndex % skins.Count)]
            : selection;
}
