using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace PixelHedgies;

public partial class App : System.Windows.Application
{
    private readonly List<Hedgehog> _hedgehogs = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly SurfaceTracker _surfaces = new();
    private Forms.NotifyIcon? _tray;
    private System.Drawing.Icon? _trayIcon;
    private long _lastTicks;
    private double _surfaceAge;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _trayIcon = TrayIconFactory.Create();
        _tray = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Pixel Hedgies - click a pet to add one",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _tray.ContextMenuStrip.Items.Add("Add animal", null, (_, _) => AddHedgehog());
        _tray.ContextMenuStrip.Items.Add("Remove all animals and exit", null, (_, _) => RemoveAllAndExit());
        AddHedgehog();
        _lastTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        _timer.Tick += Tick;
        _timer.Start();
    }

    internal void AddHedgehog(Hedgehog? parent = null)
    {
        var screen = parent is null
            ? Forms.Screen.FromPoint(Forms.Cursor.Position)
            : Forms.Screen.FromPoint(new System.Drawing.Point(
                (int)(parent.X + Hedgehog.WidthPx / 2), (int)(parent.Y + Hedgehog.HeightPx / 2)));
        var monitor = screen.Bounds;
        var rightmostX = Math.Max(monitor.Left, monitor.Right - Hedgehog.WidthPx);
        var x = Random.Shared.Next(monitor.Left, rightmostX + 1);
        // Enter from the top with enough body visible to notice the drop.
        // Feet begin below the top edge, avoiding an off-screen perch on a maximized window.
        var y = monitor.Top - Hedgehog.HeightPx + Hedgehog.HeightPx / 3;
        var selection = parent?.SkinSelection ?? AnimalSkinSelection.Hedgehog;
        var pet = new Hedgehog(this, x, y, selection);
        _hedgehogs.Add(pet);
        pet.Show();
        pet.Place();
    }

    internal void RemoveHedgehog(Hedgehog pet)
    {
        pet.DetachInteractions();
        _hedgehogs.Remove(pet);
        pet.Close();
        if (_hedgehogs.Count == 0) Shutdown();
    }

    internal void CloseAllAnimals()
    {
        foreach (var pet in _hedgehogs.ToArray())
        {
            pet.DetachInteractions();
            pet.Close();
        }
        _hedgehogs.Clear();
    }

    internal void RemoveAllAndExit()
    {
        CloseAllAnimals();
        Shutdown();
    }

    private void Tick(object? sender, EventArgs e)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var dt = Math.Min(0.05, (now - _lastTicks) / (double)System.Diagnostics.Stopwatch.Frequency);
        _lastTicks = now;
        _surfaceAge += dt;
        if (_surfaceAge >= 0.12)
        {
            _surfaces.Refresh();
            _surfaceAge = 0;
        }
        var pets = _hedgehogs.ToArray();
        foreach (var pet in pets.Where(p => !p.IsRiding)) pet.Tick(dt, _surfaces);
        foreach (var pet in pets.Where(p => p.IsRiding)) pet.Tick(dt, _surfaces);
        for (var i = 0; i < pets.Length; i++)
        {
            for (var j = i + 1; j < pets.Length; j++)
            {
                if (!pets[i].CanMeet(pets[j])) continue;
                var rider = pets[i].X <= pets[j].X ? pets[i] : pets[j];
                var host = rider == pets[i] ? pets[j] : pets[i];
                rider.StartClimb(host);
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer.Stop();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
