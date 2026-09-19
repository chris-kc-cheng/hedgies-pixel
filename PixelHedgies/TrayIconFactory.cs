using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PixelHedgies;

internal static class TrayIconFactory
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint icon);

    public static Icon Create()
    {
        var resource = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/hedgehog-actions.png"))!;
        using var source = new Bitmap(resource.Stream);
        using var small = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(small))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(source, new Rectangle(0, 4, 32, 23),
                new Rectangle(0, 100, 512, 400), GraphicsUnit.Pixel);
        }
        var handle = small.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally { DestroyIcon(handle); }
    }
}
