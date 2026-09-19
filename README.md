# Pixel Hedgies

<img src="docs/hedgehog-preview-v5.gif" width="64" height="44" alt="Retro pixel hedgehog walking with four alternating legs, blinking, looking forward, and rolling">

A tiny Windows desktop pet. Hedgehogs render at 64 × 44 pixels, with
crisp retro pixel scaling. They walk on the top edges of ordinary windows,
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

The ready-to-run Windows release is in `releases/win-x64-v8/`.
Double-click `PixelHedgies.exe` there. Keep the accompanying DLLs in the same
folder; this build does not require a separate .NET installation. The EXE is
stored with Git LFS because it exceeds GitHub's regular file-size limit. Install
Git LFS before cloning, or run `git lfs pull` after cloning to download it.
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

The current action sheet lives at `PixelHedgies/Assets/hedgehog-actions-v4.png`, and the two-frame walk strip at `PixelHedgies/Assets/hedgehog-walk-retro-v2.png`. The belly is never clipped; a dark far-side front paw is drawn behind the sprite to keep both front feet visible at 64 × 44 pixels.
The original sprite remains at `PixelHedgies/Assets/hedgehog.png`. The app intentionally
does not install itself at startup or require administrator privileges. The
hedgehogs are always on top of ordinary windows; fullscreen games and some
protected/system windows are outside the scope of this version.
