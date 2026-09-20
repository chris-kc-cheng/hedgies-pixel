using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using Path = System.Windows.Shapes.Path;
using Forms = System.Windows.Forms;

namespace PixelHedgies;

internal sealed class Hedgehog : Window
{
    internal const int WidthPx = 64;
    internal const int HeightPx = 44;
    internal const int FeetInset = 3;
    private const double RideHeight = HeightPx * 0.70;
    private const double HopDistance = WidthPx * 0.58;
    private const double Gravity = 1100;
    private const double WalkSpeed = 30;
    private enum Trick { None, Blink, Look, Roll }
    private static readonly string ImageDirectory = System.IO.Path.Combine(AppContext.BaseDirectory, "Images");
    private static readonly Lazy<BitmapSource[]> HedgehogFrames = new(LoadFrames);
    private static readonly Dictionary<string, BitmapSource[]> FileFrames = new(StringComparer.OrdinalIgnoreCase);
    private readonly App _app;
    private readonly ImageBrush _sprite;
    private readonly Border _body;
    private readonly Grid _visual;
    private readonly Canvas _footLayer;
    private readonly Path _farFrontFoot;
    private readonly Random _random = new();
    private nint _handle;
    private nint _support;
    private Native.Rect _supportBounds;
    private double _verticalSpeed;
    private double _phase;
    private double _pause;
    private double _nextTrick = 2.5;
    private double _trickRemaining;
    private double _rollAngle;
    private double _meetingCooldown;
    private double _climbElapsed;
    private double _rideRemaining;
    private double _climbStartX;
    private double _climbStartY;
    private Trick _trick;
    private int _currentFrame = -1;
    private int _direction = 1;
    private bool _dragging;
    private bool _moved;
    private Native.Point _mouseDown;
    private double _dragOffsetX;
    private double _dragOffsetY;
    private Hedgehog? _host;
    private Hedgehog? _rider;
    private string _skin;
    private Forms.ContextMenuStrip? _contextMenu;

    public double X { get; private set; }
    public double Y { get; private set; }
    internal bool IsRiding => _host is not null;
    internal string SkinSelection { get; private set; }

    public Hedgehog(App app, double x, double y, string skinSelection)
    {
        _app = app;
        SkinSelection = skinSelection;
        _skin = AnimalSkinSelection.Resolve(skinSelection, AnimalSkinSelection.Discover(ImageDirectory), Random.Shared.Next());
        X = x;
        Y = y;
        Width = WidthPx;
        Height = HeightPx;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;

        _sprite = new ImageBrush(GetFrames(_skin)[0]) { Stretch = Stretch.Uniform, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Bottom };
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        _body = new Border
        {
            Width = WidthPx,
            Height = HeightPx,
            Background = _sprite,
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
        };
        RenderOptions.SetBitmapScalingMode(_body, BitmapScalingMode.NearestNeighbor);
        _farFrontFoot = new Path
        {
            Data = Geometry.Parse("M 1,0 L 5,0 L 5,3 L 7,3 L 7,7 L 0,7 L 0,4 L 1,4 Z"),
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(119, 55, 39)),
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 19, 17)),
            StrokeThickness = 1
        };
        _footLayer = new Canvas { Width = WidthPx, Height = HeightPx, IsHitTestVisible = false };
        _footLayer.Children.Add(_farFrontFoot);
        _visual = new Grid { Width = WidthPx, Height = HeightPx, RenderTransformOrigin = new System.Windows.Point(0.5, 0.5) };
        _visual.Children.Add(_footLayer);
        _visual.Children.Add(_body);
        Content = _visual;
        ShowFrame(0);
        SourceInitialized += (_, _) =>
        {
            _handle = new WindowInteropHelper(this).Handle;
            ApplyDpiScale();
        };
        MouseLeftButtonDown += OnLeftDown;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnLeftUp;
        MouseRightButtonUp += OnRightUp;
    }

    private static BitmapSource[] GetFrames(string skin)
    {
        if (skin.Equals(AnimalSkinSelection.Hedgehog, StringComparison.OrdinalIgnoreCase)) return HedgehogFrames.Value;
        if (FileFrames.TryGetValue(skin, out var frames)) return frames;
        var firstFramePath = System.IO.Path.Combine(ImageDirectory, skin + "-frame-0.png");
        var singlePath = System.IO.Path.Combine(ImageDirectory, skin + ".png");
        var first = LoadImage(File.Exists(firstFramePath) ? firstFramePath : singlePath);
        frames = new BitmapSource[6];
        frames[0] = first;
        for (var frame = 1; frame < frames.Length; frame++)
        {
            var path = System.IO.Path.Combine(ImageDirectory, $"{skin}-frame-{frame}.png");
            frames[frame] = File.Exists(path) ? LoadImage(path) : first;
        }
        FileFrames[skin] = frames;
        return frames;
    }

    private static BitmapSource LoadImage(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapSource[] LoadFrames()
    {
        var sheet = new BitmapImage(new Uri("pack://application:,,,/Assets/hedgehog-actions-v4.png"));
        var walk = new BitmapImage(new Uri("pack://application:,,,/Assets/hedgehog-walk-retro-v2.png"));
        var walkFrameWidth = walk.PixelWidth / 2;
        var frames = new BitmapSource[6];
        for (var i = 0; i < frames.Length; i++)
        {
            var column = i % 3;
            var row = i / 3;
            frames[i] = i < 2
                ? new CroppedBitmap(walk, new Int32Rect(i * walkFrameWidth, 240,
                    i == 0 ? walkFrameWidth : walk.PixelWidth - walkFrameWidth, 620))
                : row == 0
                ? new CroppedBitmap(sheet, new Int32Rect(column * 512, 100, 512, 400))
                : new CroppedBitmap(sheet, new Int32Rect(column * 512 + 40, 542, 432, 420));
        }
        return frames;
    }

    public void Place()
    {
        ApplyDpiScale();
        if (_handle != 0)
            Native.SetWindowPos(_handle, 0, (int)Math.Round(X), (int)Math.Round(Y), WidthPx, HeightPx,
                Native.SWP_NOACTIVATE | Native.SWP_NOZORDER);
    }

    private void ApplyDpiScale()
    {
        // Physics uses physical pixels; WPF layout changes with monitor DPI.
        var fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        if (fromDevice is not { } scale) return;
        var width = WidthPx * scale.M11;
        var height = HeightPx * scale.M22;
        if (Math.Abs(Width - width) < 0.01 && Math.Abs(Height - height) < 0.01) return;
        Width = _body.Width = _visual.Width = _footLayer.Width = width;
        Height = _body.Height = _visual.Height = _footLayer.Height = height;
        PlaceFarFrontFoot(_currentFrame);
    }

    public void Tick(double dt, SurfaceTracker tracker)
    {
        if (_dragging) return;
        _phase += dt;
        _meetingCooldown = Math.Max(0, _meetingCooldown - dt);
        if (_host is not null)
        {
            TickRiding(dt);
            return;
        }
        var surfaces = tracker.Surfaces;
        var feet = Y + HeightPx - FeetInset;
        SurfaceTracker.Surface? platform = null;
        foreach (var surface in surfaces)
            if (surface.Handle == _support) { platform = surface; break; }

        // Window movement carries a perched pet, including vertical movement.
        if (platform is { } p)
        {
            X += p.Bounds.Left - _supportBounds.Left;
            _supportBounds = p.Bounds;
            if (tracker.IsExposedTop(p.Handle, X + WidthPx / 2.0))
            {
                Y = p.Bounds.Top - HeightPx + FeetInset;
                _verticalSpeed = 0;
            }
            else if (tracker.FindCoveringSurface(p.Handle, X + WidthPx / 2.0, p.Bounds.Top) is { } front)
            {
                _support = front.Handle;
                _supportBounds = front.Bounds;
                Y = front.Bounds.Top - HeightPx + FeetInset;
                _verticalSpeed = 0;
                platform = front;
            }
            else _support = 0;
        }
        else _support = 0;

        feet = Y + HeightPx - FeetInset;
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point((int)(X + WidthPx / 2), (int)feet));
        var floor = screen.Bounds.Bottom;
        if (X + WidthPx / 2.0 < screen.Bounds.Left || X + WidthPx / 2.0 >= screen.Bounds.Right)
        {
            X = Math.Clamp(X, screen.Bounds.Left - WidthPx / 2.0 + 1, screen.Bounds.Right - WidthPx / 2.0 - 1);
            _direction *= -1;
        }
        var onFloor = feet >= floor - 2 && _support == 0;
        if (onFloor) { Y = floor - HeightPx + FeetInset; _verticalSpeed = 0; }
        var grounded = _support != 0 || onFloor;
        UpdateTrick(dt, grounded);

        if (grounded)
        {
            _pause -= dt;
            if (_trick == Trick.Roll)
            {
                X += _direction * WalkSpeed * 1.5 * dt;
                _rollAngle += _direction * 480 * dt;
            }
            else if (_pause <= 0 && _trick is not (Trick.Blink or Trick.Look))
            {
                X += _direction * WalkSpeed * dt;
                if (_random.NextDouble() < dt * 0.28)
                {
                    _pause = 0.4 + _random.NextDouble() * 1.5;
                    if (_random.NextDouble() < 0.35) _direction *= -1;
                }
            }
            // Once the center passes the edge, gravity takes over next frame.
            if (_support != 0 && !tracker.IsExposedTop(_support, X + WidthPx / 2.0))
                _support = 0;
        }
        else
        {
            var oldFeet = feet;
            _verticalSpeed = Math.Min(_verticalSpeed + Gravity * dt, 850);
            Y += _verticalSpeed * dt;
            var newFeet = Y + HeightPx - FeetInset;
            var landing = tracker.FindLanding(oldFeet, newFeet, X + WidthPx / 2.0);
            if (landing is { } landed)
            {
                Y = landed.Bounds.Top - HeightPx + FeetInset;
                _verticalSpeed = 0;
                _support = landed.Handle;
                _supportBounds = landed.Bounds;
                _pause = 0.2;
            }
            else if (newFeet >= floor)
            {
                Y = floor - HeightPx + FeetInset;
                _verticalSpeed = 0;
            }
        }

        var walking = grounded && _pause <= 0 && _trick == Trick.None;
        var frame = _trick switch
        {
            Trick.Blink => 2,
            Trick.Look => 3,
            Trick.Roll => 4,
            _ => walking ? (int)(_phase * 8) % 2 : 5
        };
        ShowFrame(frame);
        _visual.RenderTransform = new TransformGroup
        {
            Children = new TransformCollection
            {
                new ScaleTransform(_trick is Trick.Look or Trick.Roll ? 1 : _direction, 1),
                new RotateTransform(_trick == Trick.Roll ? _rollAngle : 0)
            }
        };
        _grounded = _support != 0 || Math.Abs(Y + HeightPx - FeetInset - floor) <= 2;
        Place();
    }

    private bool _grounded;

    internal bool CanMeet(Hedgehog other) =>
        _grounded && other._grounded && !_dragging && !other._dragging &&
        _host is null && other._host is null && _rider is null && other._rider is null &&
        _meetingCooldown <= 0 && other._meetingCooldown <= 0 &&
        _trick != Trick.Roll && other._trick != Trick.Roll &&
        MeetingRules.CloseEnough(X, Y, other.X, other.Y, WidthPx, HeightPx);

    internal void StartClimb(Hedgehog host)
    {
        _host = host;
        host._rider = this;
        _meetingCooldown = host._meetingCooldown = 4;
        _climbElapsed = 0;
        _rideRemaining = 2.1;
        _climbStartX = X;
        _climbStartY = Y;
        _direction = X <= host.X ? 1 : -1;
        _support = 0;
        _grounded = false;
        _verticalSpeed = 0;
        _trick = Trick.None;
        // The rider must be drawn above the hedgehog it is climbing.
        if (_handle != 0)
            Native.SetWindowPos(_handle, (nint)(-1), (int)X, (int)Y, WidthPx, HeightPx, Native.SWP_NOACTIVATE);
    }

    internal void DetachInteractions()
    {
        _contextMenu?.Close();
        _rider?.LeaveHost(true);
        if (_host is not null) LeaveHost(false);
    }

    private void TickRiding(double dt)
    {
        var host = _host!;
        if (!host.IsVisible || host._dragging)
        {
            LeaveHost(true);
            return;
        }
        _climbElapsed += dt;
        _rideRemaining -= dt;
        var progress = Math.Clamp(_climbElapsed / 0.38, 0, 1);
        var eased = progress * progress * (3 - 2 * progress);
        X = _climbStartX + (host.X - _climbStartX) * eased;
        Y = _climbStartY + (host.Y - RideHeight - _climbStartY) * eased;
        if (progress >= 1)
        {
            X = host.X;
            Y = host.Y - RideHeight + Math.Sin(_phase * 12) * 1.5;
        }
        ShowFrame((int)(_phase * 8) % 2);
        _visual.RenderTransform = new TransformGroup
        {
            Children = new TransformCollection
            {
                new ScaleTransform(_direction, 1),
                new RotateTransform(-12 * Math.Sin(Math.PI * progress))
            }
        };
        Place();
        if (_rideRemaining <= 0) LeaveHost(true);
    }

    private void LeaveHost(bool hop)
    {
        var host = _host;
        if (host is null) return;
        host._rider = null;
        host._meetingCooldown = Math.Max(host._meetingCooldown, 2.5);
        _host = null;
        _support = 0;
        _grounded = false;
        _meetingCooldown = Math.Max(_meetingCooldown, 2.5);
        _verticalSpeed = hop ? -180 : 0;
        if (hop)
        {
            X = host.X + _direction * HopDistance;
            Y = host.Y - RideHeight;
            Place();
        }
    }

    private void ShowFrame(int frame)
    {
        if (frame == _currentFrame) return;
        _sprite.ImageSource = GetFrames(_skin)[frame];
        _currentFrame = frame;
        PlaceFarFrontFoot(frame);
    }

    private void PlaceFarFrontFoot(int frame)
    {
        _farFrontFoot.Visibility = _skin.Equals(AnimalSkinSelection.Hedgehog, StringComparison.OrdinalIgnoreCase) && frame is 0 or 1 or 5
            ? Visibility.Visible
            : Visibility.Hidden;
        var scale = _body.Width / WidthPx;
        _farFrontFoot.RenderTransform = new ScaleTransform(scale, scale);
        Canvas.SetLeft(_farFrontFoot, (frame == 1 ? 38 : frame == 5 ? 35 : 32) * scale);
        Canvas.SetTop(_farFrontFoot, 36 * scale);
    }

    private void UpdateTrick(double dt, bool grounded)
    {
        if (!grounded)
        {
            _trick = Trick.None;
            _trickRemaining = 0;
            return;
        }
        if (_trick != Trick.None)
        {
            _trickRemaining -= dt;
            if (_trickRemaining > 0) return;
            _trick = Trick.None;
            _nextTrick = 2 + _random.NextDouble() * 5;
        }
        _nextTrick -= dt;
        if (_nextTrick > 0) return;
        var choice = _random.NextDouble();
        _trick = choice < 0.6 ? Trick.Blink : choice < 0.88 ? Trick.Look : Trick.Roll;
        _trickRemaining = _trick switch
        {
            Trick.Blink => 0.14,
            Trick.Look => 1.1,
            _ => 0.85
        };
        _nextTrick = 0;
    }

    private void OnLeftDown(object sender, MouseButtonEventArgs e)
    {
        if (!Native.GetCursorPos(out _mouseDown)) return;
        _dragOffsetX = _mouseDown.X - X;
        _dragOffsetY = _mouseDown.Y - Y;
        _moved = false;
        _dragging = true;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging || !Native.GetCursorPos(out var cursor)) return;
        if (!_moved && Math.Abs(cursor.X - _mouseDown.X) + Math.Abs(cursor.Y - _mouseDown.Y) > 6)
        {
            _moved = true;
            DetachInteractions();
        }
        if (!_moved) return;
        X = cursor.X - _dragOffsetX;
        Y = cursor.Y - _dragOffsetY;
        Place();
    }

    private void OnLeftUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();
        if (_moved) { _support = 0; _verticalSpeed = 0; }
        else _app.AddHedgehog(this);
        e.Handled = true;
    }

    private void OnRightUp(object sender, MouseButtonEventArgs e)
    {
        _contextMenu?.Close();
        var menu = _contextMenu = new Forms.ContextMenuStrip();
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_contextMenu, menu)) _contextMenu = null;
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(menu.Dispose));
        };
        var skins = new Forms.ToolStripMenuItem("Change animal skin");
        foreach (var choice in AnimalSkinSelection.Discover(ImageDirectory).Append(AnimalSkinSelection.Random))
        {
            var item = new Forms.ToolStripMenuItem(choice.ToString())
            {
                Checked = SkinSelection.Equals(choice, StringComparison.OrdinalIgnoreCase)
            };
            item.Click += (_, _) => RunAfterMenuCloses(() => ChangeSkin(choice));
            skins.DropDownItems.Add(item);
        }
        menu.Items.Add(skins);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Remove this animal", null, (_, _) => RunAfterMenuCloses(() => _app.RemoveHedgehog(this)));
        menu.Items.Add("Remove all animals and exit", null, (_, _) => RunAfterMenuCloses(_app.RemoveAllAndExit));
        menu.Show(Forms.Cursor.Position);
        e.Handled = true;
    }

    private void RunAfterMenuCloses(Action action) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

    private void ChangeSkin(string selection)
    {
        SkinSelection = selection;
        _skin = AnimalSkinSelection.Resolve(selection, AnimalSkinSelection.Discover(ImageDirectory), Random.Shared.Next());
        _currentFrame = -1;
        ShowFrame(0);
    }
}
