using PixelHedgies;

static Native.Rect Rect(int left, int top, int right, int bottom) =>
    new() { Left = left, Top = top, Right = right, Bottom = bottom };

var front = new SurfaceTracker.Surface(2, Rect(100, 60, 300, 260), true);
var back = new SurfaceTracker.Surface(1, Rect(0, 100, 400, 350), true);
var tracker = new SurfaceTracker([front, back]);

Check(tracker.IsExposedTop(2, 150), "front edge is exposed");
Check(!tracker.IsExposedTop(1, 150), "covered back edge is hidden");
Check(tracker.IsExposedTop(1, 50), "uncovered portion of back edge is exposed");
Check(tracker.FindLanding(0, 120, 150)?.Handle == 2, "fall lands on front window");
Check(tracker.FindLanding(0, 120, 50)?.Handle == 1, "fall lands on exposed back segment");
Check(tracker.FindCoveringSurface(1, 150, 100)?.Handle == 2, "covered pet transfers forward");

var popup = new SurfaceTracker.Surface(3, Rect(100, 80, 300, 200), false);
var popupTracker = new SurfaceTracker([popup, back]);
Check(!popupTracker.IsExposedTop(1, 150), "popup obscures back edge");
Check(popupTracker.FindLanding(0, 120, 150) is null, "popup is not a perch");

var taskbar = Rect(0, 1040, 1920, 1080);
Check(SurfaceTracker.CanSupportWindow("Shell_TrayWnd", taskbar, true), "primary taskbar is a perch");
Check(SurfaceTracker.CanSupportWindow("Shell_SecondaryTrayWnd", taskbar, true), "secondary taskbar is a perch");
Check(!SurfaceTracker.CanSupportWindow("Shell_TrayWnd", Rect(0, 0, 40, 1080), true), "vertical taskbar is not a top-edge perch");

Check(MeetingRules.CloseEnough(100, 200, 145, 204, 80, 56), "nearby pets can meet");
Check(!MeetingRules.CloseEnough(100, 200, 160, 204, 80, 56), "distant pets do not meet");
Check(!MeetingRules.CloseEnough(100, 200, 145, 230, 80, 56), "pets on different heights do not meet");

Console.WriteLine("All geometry checks passed.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
