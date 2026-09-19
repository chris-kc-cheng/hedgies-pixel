namespace PixelHedgies;

internal static class MeetingRules
{
    // Positions are the top-left corners of equally sized pet windows.
    internal static bool CloseEnough(double firstX, double firstY, double secondX, double secondY,
        double spriteWidth, double spriteHeight) =>
        Math.Abs(firstX - secondX) <= spriteWidth * 0.60 &&
        Math.Abs(firstY - secondY) <= spriteHeight * 0.16;
}
