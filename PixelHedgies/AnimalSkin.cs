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
            var name = Path.GetFileNameWithoutExtension(path);
            if (name.EndsWith("-frame-1", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.EndsWith("-frame-0", StringComparison.OrdinalIgnoreCase))
                name = name[..^8];
            if (name.Equals(Random, StringComparison.OrdinalIgnoreCase) ||
                skins.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            skins.Add(name);
        }
        return skins;
    }

    internal static string Resolve(string selection, IReadOnlyList<string> skins, int randomIndex) =>
        selection.Equals(Random, StringComparison.OrdinalIgnoreCase)
            ? skins[(int)((uint)randomIndex % skins.Count)]
            : selection;
}
