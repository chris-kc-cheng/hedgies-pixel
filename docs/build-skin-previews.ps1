Add-Type -AssemblyName System.Drawing

$previewDir = Join-Path $PSScriptRoot 'skin-previews'
$gifFramesDir = Join-Path (Split-Path -Parent $PSScriptRoot) '.tools/skin-preview-frames'
New-Item -ItemType Directory -Path $gifFramesDir -Force | Out-Null
foreach ($skin in @('poodle', 'labubu')) {
    $sourcePath = Join-Path $previewDir "$skin-walk.png"
    $source = [System.Drawing.Bitmap]::new($sourcePath)
    try {
        $halfWidth = [int][Math]::Floor($source.Width / 2)
        $bounds = @()
        for ($frame = 0; $frame -lt 2; $frame++) {
            $left = if ($frame -eq 0) { 0 } else { $halfWidth }
            $right = if ($frame -eq 0) { $halfWidth } else { $source.Width }
            $minX = $right; $minY = $source.Height
            $maxX = -1; $maxY = -1
            for ($y = 0; $y -lt $source.Height; $y++) {
                for ($x = $left; $x -lt $right; $x++) {
                    if ($source.GetPixel($x, $y).A -le 10) { continue }
                    $minX = [Math]::Min($minX, $x)
                    $minY = [Math]::Min($minY, $y)
                    $maxX = [Math]::Max($maxX, $x)
                    $maxY = [Math]::Max($maxY, $y)
                }
            }
            if ($maxX -lt $minX) { throw "No visible pixels in $skin frame $frame" }
            $bounds += [System.Drawing.Rectangle]::new($minX, $minY, $maxX - $minX + 1, $maxY - $minY + 1)
        }

        $maxWidth = [Math]::Max($bounds[0].Width, $bounds[1].Width)
        $maxHeight = [Math]::Max($bounds[0].Height, $bounds[1].Height)
        $scale = [Math]::Min(60.0 / $maxWidth, 40.0 / $maxHeight)
        for ($frame = 0; $frame -lt 2; $frame++) {
            $crop = $bounds[$frame]
            $width = [int][Math]::Round($crop.Width * $scale)
            $height = [int][Math]::Round($crop.Height * $scale)
            $target = [System.Drawing.Rectangle]::new([int][Math]::Floor((64 - $width) / 2), 42 - $height, $width, $height)
            $canvas = [System.Drawing.Bitmap]::new(64, 44, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [System.Drawing.Graphics]::FromImage($canvas)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $graphics.DrawImage($source, $target, $crop, [System.Drawing.GraphicsUnit]::Pixel)
                # The generated walk poses barely move at 64x44. Replace the feet
                # with deliberate opposing steps so the preview shows a readable gait.
                $outline = [System.Drawing.Color]::FromArgb(40, 23, 18)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                if ($skin -eq 'poodle') {
                    $graphics.FillRectangle([System.Drawing.Brushes]::Transparent, 12, 39, 40, 4)
                    $legs = if ($frame -eq 0) {
                        @(@(18, 36, 15, 39, 6, 2), @(41, 36, 45, 39, 6, 2))
                    } else {
                        @(@(18, 36, 21, 39, 6, 2), @(41, 36, 35, 39, 6, 2))
                    }
                    $fill = [System.Drawing.Color]::FromArgb(246, 209, 167)
                } else {
                    $graphics.FillRectangle([System.Drawing.Brushes]::Transparent, 20, 38, 22, 5)
                    $legs = if ($frame -eq 0) {
                        @(@(26, 35, 22, 39, 6, 2), @(34, 35, 36, 39, 6, 2))
                    } else {
                        @(@(26, 35, 29, 39, 6, 2), @(34, 35, 29, 39, 6, 2))
                    }
                    $fill = [System.Drawing.Color]::FromArgb(170, 103, 69)
                }
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
                foreach ($leg in $legs) {
                    $hipX, $hipY, $footX, $footY, $footWidth, $footHeight = $leg
                    $points = [System.Drawing.Point[]]@(
                        [System.Drawing.Point]::new($hipX - 2, $hipY),
                        [System.Drawing.Point]::new($hipX + 3, $hipY),
                        [System.Drawing.Point]::new($footX + $footWidth - 2, $footY),
                        [System.Drawing.Point]::new($footX + $footWidth, $footY + $footHeight),
                        [System.Drawing.Point]::new($footX, $footY + $footHeight),
                        [System.Drawing.Point]::new($footX, $footY)
                    )
                    $brush = [System.Drawing.SolidBrush]::new($fill)
                    $pen = [System.Drawing.Pen]::new($outline, 1)
                    try {
                        $graphics.FillPolygon($brush, $points)
                        $graphics.DrawPolygon($pen, $points)
                    } finally {
                        $brush.Dispose()
                        $pen.Dispose()
                    }
                }
                $canvas.Save((Join-Path $previewDir "$skin-frame-$frame.png"), [System.Drawing.Imaging.ImageFormat]::Png)
                $preview = [System.Drawing.Bitmap]::new(64, 44, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                $previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
                try {
                    $previewGraphics.Clear([System.Drawing.Color]::FromArgb(238, 242, 245))
                    $previewGraphics.DrawImageUnscaled($canvas, 0, 0)
                    $preview.Save((Join-Path $gifFramesDir "$skin-frame-$frame.png"), [System.Drawing.Imaging.ImageFormat]::Png)
                } finally {
                    $previewGraphics.Dispose()
                    $preview.Dispose()
                }
            } finally {
                $graphics.Dispose()
                $canvas.Dispose()
            }
        }
    } finally {
        $source.Dispose()
    }

    & ffmpeg -hide_banner -loglevel error -y -framerate 4 -i (Join-Path $gifFramesDir "$skin-frame-%d.png") `
        -filter_complex '[0:v]scale=256:176:flags=neighbor,split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse' `
        -loop 0 (Join-Path $previewDir "$skin-preview.gif")
    if ($LASTEXITCODE -ne 0) { throw "GIF encoding failed for $skin" }
}
