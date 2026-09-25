Add-Type -AssemblyName System.Drawing

$repo = Split-Path -Parent $PSScriptRoot
$previewDir = Join-Path $PSScriptRoot 'skin-previews'
$imagesDir = Join-Path $repo 'PixelHedgies/Images'
$releaseImagesDir = Join-Path $repo 'releases/win-x64-v14/Images'
$gifFramesDir = Join-Path $repo '.tools/skin-preview-frames'
New-Item -ItemType Directory -Path $gifFramesDir -Force | Out-Null

foreach ($skin in @('Poodle', 'Capybara', 'Rabbit', 'Beaver')) {
    $key = $skin.ToLowerInvariant()
    $sourcePath = Join-Path $previewDir "$key-walk.png"
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

        # One scale for both poses prevents apparent body-size changes.
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
                $frameName = "$skin-frame-$frame.png"
                $canvas.Save((Join-Path $imagesDir $frameName), [System.Drawing.Imaging.ImageFormat]::Png)
                $canvas.Save((Join-Path $releaseImagesDir $frameName), [System.Drawing.Imaging.ImageFormat]::Png)
                $canvas.Save((Join-Path $gifFramesDir "$key-frame-$frame.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            } finally {
                $graphics.Dispose()
                $canvas.Dispose()
            }
        }
    } finally {
        $source.Dispose()
    }

    & ffmpeg -hide_banner -loglevel error -y -framerate 4 -i (Join-Path $gifFramesDir "$key-frame-%d.png") `
        -filter_complex '[0:v]scale=256:176:flags=neighbor,split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse' `
        -loop 0 (Join-Path $previewDir "$key-preview.gif")
    if ($LASTEXITCODE -ne 0) { throw "GIF encoding failed for $skin" }
}
