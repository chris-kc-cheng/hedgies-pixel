using System.Runtime.InteropServices;
using System.Text;

namespace PixelHedgies;

internal sealed class SurfaceTracker
{
    internal readonly record struct Surface(nint Handle, Native.Rect Bounds, bool CanSupport);
    public IReadOnlyList<Surface> Surfaces { get; private set; } = [];

    internal SurfaceTracker(IReadOnlyList<Surface>? initialSurfaces = null)
    {
        if (initialSurfaces is not null) Surfaces = initialSurfaces;
    }

    public Surface? FindLanding(double oldFeet, double newFeet, double centerX)
    {
        Surface? landing = null;
        for (var i = 0; i < Surfaces.Count; i++)
        {
            var surface = Surfaces[i];
            var top = surface.Bounds.Top;
            if (!surface.CanSupport || oldFeet > top || newFeet < top ||
                !IsExposedTop(i, centerX)) continue;
            if (landing is null || top < landing.Value.Bounds.Top) landing = surface;
        }
        return landing;
    }

    public bool IsExposedTop(nint handle, double centerX)
    {
        for (var i = 0; i < Surfaces.Count; i++)
            if (Surfaces[i].Handle == handle) return IsExposedTop(i, centerX);
        return false;
    }

    public Surface? FindCoveringSurface(nint coveredHandle, double centerX, int coveredTop)
    {
        for (var i = 0; i < Surfaces.Count; i++)
        {
            var front = Surfaces[i];
            if (front.Handle == coveredHandle) break;
            if (front.CanSupport && centerX >= front.Bounds.Left && centerX < front.Bounds.Right &&
                coveredTop >= front.Bounds.Top && coveredTop < front.Bounds.Bottom &&
                IsExposedTop(i, centerX)) return front;
        }
        return null;
    }

    private bool IsExposedTop(int index, double centerX)
    {
        var surface = Surfaces[index];
        var top = surface.Bounds.Top;
        if (centerX < surface.Bounds.Left || centerX >= surface.Bounds.Right) return false;
        // Earlier windows in the list are in front. An opaque front rectangle
        // covering the top edge makes this perch invisible at the pet's feet.
        for (var i = 0; i < index; i++)
        {
            var front = Surfaces[i].Bounds;
            if (centerX >= front.Left && centerX < front.Right &&
                top >= front.Top && top < front.Bottom) return false;
        }
        return true;
    }

    internal static bool CanSupportWindow(string className, Native.Rect rect, bool isTool)
    {
        // Explorer's taskbar can be much shorter than a normal app window.
        // The secondary class is used on additional monitors.
        if (className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
            return rect.Width >= 120 && rect.Width > rect.Height * 2 && rect.Height >= 20;
        return !isTool && rect.Width >= 120 && rect.Height >= 60;
    }

    public void Refresh()
    {
        var found = new List<Surface>();
        var visited = new HashSet<nint>();
        for (var handle = Native.GetTopWindow(0); handle != 0 && visited.Add(handle) && visited.Count < 4096;
             handle = Native.GetWindow(handle, Native.GW_HWNDNEXT))
        {
            if (!Native.IsWindowVisible(handle) || Native.IsIconic(handle)) continue;
            Native.GetWindowThreadProcessId(handle, out var pid);
            if (pid == Environment.ProcessId) continue;
            if (Native.DwmGetWindowAttribute(handle, Native.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0) continue;
            if (Native.DwmGetWindowAttribute(handle, Native.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out Native.Rect rect, Marshal.SizeOf<Native.Rect>()) != 0 && !Native.GetWindowRect(handle, out rect)) continue;
            if (rect.Width < 30 || rect.Height < 20) continue;
            var className = new StringBuilder(128);
            Native.GetClassName(handle, className, className.Capacity);
            var windowClass = className.ToString();
            if (windowClass is "Progman" or "WorkerW") continue;
            var isTool = ((long)Native.GetWindowLongPtr(handle, Native.GWL_EXSTYLE) & Native.WS_EX_TOOLWINDOW) != 0;
            var canSupport = CanSupportWindow(windowClass, rect, isTool);
            found.Add(new Surface(handle, rect, canSupport));
        }
        Surfaces = found;
    }
}
