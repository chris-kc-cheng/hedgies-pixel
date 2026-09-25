Add-Type -AssemblyName System.Drawing

$repo = Split-Path -Parent $PSScriptRoot
$sheetPath = Join-Path $repo 'PixelHedgies/Assets/hedgehog-actions-v4.png'
$walkPath = Join-Path $repo 'PixelHedgies/Assets/hedgehog-walk-retro-v2.png'
$framesDir = Join-Path $repo '.tools/preview-frames-v5'
New-Item -ItemType Directory -Path $framesDir -Force | Out-Null
$sheet = [System.Drawing.Bitmap]::new($sheetPath)
$walk = [System.Drawing.Bitmap]::new($walkPath)
$walkFrameWidth = [int][Math]::Floor($walk.Width / 2)
$footFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(119, 55, 39))
$footOutline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(32, 19, 17), 1)
# Every pose is rendered directly from the sheet, exactly like the application.
$poses = @(
    'walk0', 'walk1', 'walk0', 'walk1', 'walk0', 'walk1',
    'blink', 'walk0', 'walk1', 'look', 'look', 'look',
    'roll0', 'roll90', 'roll180', 'roll270', 'idle', 'idle'
)
try {
    for ($i = 0; $i -lt $poses.Count; $i++) {
        $pose = $poses[$i]
        $canvas = [System.Drawing.Bitmap]::new(64, 44, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $g.Clear([System.Drawing.Color]::Transparent)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            if ($pose -match '^(walk|idle)') {
                if ($pose -eq 'idle') {
                    $footX = 35
                    $footPoints = [System.Drawing.Point[]]@(
                        [System.Drawing.Point]::new(($footX + 1), 36),
                        [System.Drawing.Point]::new(($footX + 5), 36),
                        [System.Drawing.Point]::new(($footX + 5), 39),
                        [System.Drawing.Point]::new(($footX + 7), 39),
                        [System.Drawing.Point]::new(($footX + 7), 43),
                        [System.Drawing.Point]::new($footX, 43),
                        [System.Drawing.Point]::new($footX, 40)
                    )
                    $g.FillPolygon($footFill, $footPoints)
                    $g.DrawPolygon($footOutline, $footPoints)
                    $source = [System.Drawing.Rectangle]::new(1064, 542, 432, 420)
                    $target = [System.Drawing.Rectangle]::new(9, 0, 46, 44)
                } else {
                    $column = if ($pose -eq 'walk0') { 0 } else { 1 }
                    $sourceWidth = if ($column -eq 0) { $walkFrameWidth } else { $walk.Width - $walkFrameWidth }
                    $source = [System.Drawing.Rectangle]::new(($column * $walkFrameWidth), 240, $sourceWidth, 620)
                    $target = [System.Drawing.Rectangle]::new(4, 0, 56, 44)
                }
                $sourceImage = if ($pose -eq 'idle') { $sheet } else { $walk }
                $g.DrawImage($sourceImage, $target, $source, [System.Drawing.GraphicsUnit]::Pixel)
            } else {
                $column = if ($pose -eq 'blink') { 2 } elseif ($pose -eq 'look') { 0 } else { 1 }
                if ($pose -eq 'blink') {
                    $source = [System.Drawing.Rectangle]::new(1024, 100, 512, 400)
                    $target = [System.Drawing.Rectangle]::new(4, 0, 56, 44)
                } else {
                    $source = [System.Drawing.Rectangle]::new(($column * 512 + 40), 542, 432, 420)
                    $target = [System.Drawing.Rectangle]::new(9, 0, 46, 44)
                }
                if ($pose -match '^roll') {
                    $g.TranslateTransform(32, 22)
                    $g.RotateTransform([float]($pose.Substring(4)))
                    $g.TranslateTransform(-32, -22)
                }
                $g.DrawImage($sheet, $target, $source, [System.Drawing.GraphicsUnit]::Pixel)
            }
            $canvas.Save((Join-Path $framesDir ('frame-{0:D2}.png' -f $i)),
                [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $g.Dispose()
            $canvas.Dispose()
        }
    }
} finally {
    $footFill.Dispose()
    $footOutline.Dispose()
    $sheet.Dispose()
    $walk.Dispose()
}

& ffmpeg -hide_banner -loglevel error -y -framerate 8 -i (Join-Path $framesDir 'frame-%02d.png') `
    -filter_complex '[0:v]split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse' `
    -loop 0 (Join-Path $PSScriptRoot 'hedgehog-preview-v5.gif')
if ($LASTEXITCODE -ne 0) { throw 'GIF encoding failed.' }
