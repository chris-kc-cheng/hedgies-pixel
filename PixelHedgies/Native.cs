using System.Runtime.InteropServices;
using System.Text;

namespace PixelHedgies;

internal static class Native
{
    internal const int GWL_EXSTYLE = -20;
    internal const long WS_EX_TOOLWINDOW = 0x00000080;
    internal const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    internal const int DWMWA_CLOAKED = 14;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_NOZORDER = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point { public int X, Y; }

    internal const uint GW_HWNDNEXT = 2;

    [DllImport("user32.dll")] internal static extern nint GetTopWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint GetWindow(nint hwnd, uint command);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(nint hwnd, StringBuilder name, int maxCount);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] internal static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out Rect rect, int size);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);
}
