# Pixel Hedgies

<img src="docs/hedgehog-preview.gif" width="80" height="56" alt="Animated pixel hedgehog walking, blinking, looking forward, and rolling">

A tiny Windows desktop pet. Hedgehogs now render at 80 × 56 pixels, with
smoother downscaling. They walk on the top edges of ordinary windows,
including the Windows 11 taskbar, fall when they wander off, and land on another
window or the monitor's bottom edge. When two meet, one scrambles onto the
other's back, rides briefly, then hops away.

## Controls

- Left-click a hedgehog: add one.
- New hedgehogs appear at a random position along the top of that monitor and fall in.
- Drag a hedgehog: relocate it.
- Right-click a hedgehog: remove it. Removing the last hedgehog exits.
- The notification-area menu also offers **Add hedgehog** and **Exit**.

## Build and run

If you built a release locally, the latest ready-to-run build is in
`PixelHedgies/bin/Release/net10.0-windows/win-x64/publish-v6/`.
Double-click `PixelHedgies.exe` there. Keep the accompanying DLLs in the same
folder; this build does not require a separate .NET installation. Published
binaries are not stored in Git, so a fresh clone must build or publish first.
Exit any older Pixel Hedgies instance from its notification-area icon before
starting this one.

To build from source, install the .NET 10 SDK on Windows, then run:

```powershell
dotnet run --project PixelHedgies/PixelHedgies.csproj
```

The window-overlap geometry checks can be run with:

```powershell
dotnet run --project PixelHedgies.GeometryChecks/PixelHedgies.GeometryChecks.csproj
```

To publish a self-contained Windows build:

```powershell
dotnet publish PixelHedgies/PixelHedgies.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The animated sprite sheet lives at `PixelHedgies/Assets/hedgehog-actions.png`.
The original sprite remains at `PixelHedgies/Assets/hedgehog.png`. The app intentionally
does not install itself at startup or require administrator privileges. The
hedgehogs are always on top of ordinary windows; fullscreen games and some
protected/system windows are outside the scope of this version.
