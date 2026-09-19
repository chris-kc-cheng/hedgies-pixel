namespace PixelHedgies;

internal enum AnimalSkin
{
    Hedgehog,
    Poodle,
    Labubu,
    Random
}

internal static class AnimalSkinSelection
{
    internal static readonly AnimalSkin[] ConcreteSkins =
        [AnimalSkin.Hedgehog, AnimalSkin.Poodle, AnimalSkin.Labubu];

    internal static AnimalSkin Resolve(AnimalSkin selection, int randomIndex) =>
        selection == AnimalSkin.Random
            ? ConcreteSkins[(int)((uint)randomIndex % ConcreteSkins.Length)]
            : selection;
}
