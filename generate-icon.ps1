<#
.SYNOPSIS
    Generates Assets/AdminToolbox.ico (multi-resolution: 256, 64, 48, 32, 16 px)
    using System.Drawing — no external tools required.
#>

Add-Type -AssemblyName System.Drawing

# ---------------------------------------------------------------------------
# Draw one bitmap of the given size
# ---------------------------------------------------------------------------
function New-IconBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $cx = $Size / 2.0
    $cy = $Size / 2.0

    # ---- background: blue rounded square ----
    $pad = [Math]::Max(1, [int]($Size * 0.05))
    $x   = [float]$pad
    $y   = [float]$pad
    $w   = [float]($Size - 2 * $pad)
    $h   = [float]($Size - 2 * $pad)
    $rr  = [float]($Size * 0.22)

    $bgPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bgPath.AddArc($x,           $y,           $rr, $rr, 180, 90)
    $bgPath.AddArc($x + $w - $rr,$y,           $rr, $rr, 270, 90)
    $bgPath.AddArc($x + $w - $rr,$y + $h - $rr,$rr, $rr,   0, 90)
    $bgPath.AddArc($x,           $y + $h - $rr,$rr, $rr,  90, 90)
    $bgPath.CloseFigure()

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.RectangleF]::new($x, $y, $w, $h),
        [System.Drawing.Color]::FromArgb(255,  0, 110, 195),
        [System.Drawing.Color]::FromArgb(255,  0,  55, 120),
        [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal
    )
    $g.FillPath($bgBrush, $bgPath)

    # ---- gear ----
    $nTeeth    = 8
    $outerR    = $Size * 0.335
    $innerR    = $Size * 0.230
    $toothDeg  = 360.0 / $nTeeth     # 45°
    $toothSpan = $toothDeg * 0.55    # tooth-top arc  (~24.75°)
    $valleySpan= $toothDeg * 0.45   # valley arc     (~20.25°)

    $gearPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $gearPath.FillMode = [System.Drawing.Drawing2D.FillMode]::Winding

    $angle = -90.0 - ($toothSpan / 2.0)   # centre first tooth at top

    for ($i = 0; $i -lt $nTeeth; $i++) {
        # outer arc (tooth top)
        $gearPath.AddArc(
            [float]($cx - $outerR), [float]($cy - $outerR),
            [float]($outerR * 2.0), [float]($outerR * 2.0),
            [float]$angle, [float]$toothSpan
        )
        $angle += $toothSpan

        # inner arc (valley between teeth)
        $gearPath.AddArc(
            [float]($cx - $innerR), [float]($cy - $innerR),
            [float]($innerR * 2.0), [float]($innerR * 2.0),
            [float]$angle, [float]$valleySpan
        )
        $angle += $valleySpan
    }
    $gearPath.CloseFigure()

    $whiteBrush = New-Object System.Drawing.SolidBrush(
        [System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    $g.FillPath($whiteBrush, $gearPath)

    # ---- shield overlay (matches reference: large outlined shield ON TOP of gear) ----
    # The shield is drawn as a white-outlined dark-blue shape overlaid on the gear.
    # Shape: slight arch at top, slightly concave sides tapering to a smooth pointed bottom.
    $shieldDark  = New-Object System.Drawing.SolidBrush(
        [System.Drawing.Color]::FromArgb(255, 0, 55, 120))
    $outlinePen  = New-Object System.Drawing.Pen(
        [System.Drawing.Color]::FromArgb(245, 255, 255, 255), [float]($Size * 0.022))
    $outlinePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    # Shield dimensions (large — overlaps gear ring)
    [double]$shw   = $Size * 0.195    # half-width
    [double]$shTop = $Size * 0.225    # distance above cy to top
    [double]$shBot = $Size * 0.265    # distance below cy to tip
    [double]$shMid = $Size * 0.060    # where sides start tapering

    # Build the shield path using Bezier curves for smooth contour
    $shield = New-Object System.Drawing.Drawing2D.GraphicsPath

    # --- top arch (subtle upward curve) ---
    $shield.AddBezier(
        [float]($cx - $shw), [float]($cy - $shTop + $Size * 0.03),  # TL (slightly lowered)
        [float]($cx - $shw * 0.4), [float]($cy - $shTop - $Size * 0.01),  # CP left
        [float]($cx + $shw * 0.4), [float]($cy - $shTop - $Size * 0.01),  # CP right
        [float]($cx + $shw), [float]($cy - $shTop + $Size * 0.03)   # TR (slightly lowered)
    )

    # --- right side: slight concave curve from top-right to shoulder ---
    $shield.AddBezier(
        [float]($cx + $shw),          [float]($cy - $shTop + $Size * 0.03),
        [float]($cx + $shw * 0.97),   [float]($cy - $Size * 0.02),
        [float]($cx + $shw * 0.95),   [float]($cy + $shMid * 0.5),
        [float]($cx + $shw * 0.85),   [float]($cy + $shMid)
    )

    # --- bottom right to tip ---
    $shield.AddBezier(
        [float]($cx + $shw * 0.85),  [float]($cy + $shMid),
        [float]($cx + $shw * 0.45),  [float]($cy + $shBot * 0.65),
        [float]($cx + $Size * 0.02), [float]($cy + $shBot * 0.95),
        [float]$cx,                   [float]($cy + $shBot)
    )

    # --- tip to bottom left ---
    $shield.AddBezier(
        [float]$cx,                   [float]($cy + $shBot),
        [float]($cx - $Size * 0.02), [float]($cy + $shBot * 0.95),
        [float]($cx - $shw * 0.45),  [float]($cy + $shBot * 0.65),
        [float]($cx - $shw * 0.85),  [float]($cy + $shMid)
    )

    # --- left side: slight concave curve from shoulder to top-left ---
    $shield.AddBezier(
        [float]($cx - $shw * 0.85),  [float]($cy + $shMid),
        [float]($cx - $shw * 0.95),  [float]($cy + $shMid * 0.5),
        [float]($cx - $shw * 0.97),  [float]($cy - $Size * 0.02),
        [float]($cx - $shw),         [float]($cy - $shTop + $Size * 0.03)
    )
    $shield.CloseFigure()

    # Draw: fill dark blue, then stroke white outline
    $g.FillPath($shieldDark, $shield)
    $g.DrawPath($outlinePen, $shield)

    # --- inner shield outline (smaller, offset inward by stroke width * 2) ---
    [double]$inset = $Size * 0.032
    [double]$ihw   = $shw - $inset
    [double]$iTop  = $shTop - $inset
    [double]$iBot  = $shBot - $inset
    [double]$iMid  = $shMid - $inset * 0.3

    $innerShield = New-Object System.Drawing.Drawing2D.GraphicsPath

    $innerShield.AddBezier(
        [float]($cx - $ihw), [float]($cy - $iTop + $Size * 0.03),
        [float]($cx - $ihw * 0.4), [float]($cy - $iTop - $Size * 0.005),
        [float]($cx + $ihw * 0.4), [float]($cy - $iTop - $Size * 0.005),
        [float]($cx + $ihw), [float]($cy - $iTop + $Size * 0.03)
    )
    $innerShield.AddBezier(
        [float]($cx + $ihw),        [float]($cy - $iTop + $Size * 0.03),
        [float]($cx + $ihw * 0.97), [float]($cy - $Size * 0.01),
        [float]($cx + $ihw * 0.95), [float]($cy + $iMid * 0.5),
        [float]($cx + $ihw * 0.85), [float]($cy + $iMid)
    )
    $innerShield.AddBezier(
        [float]($cx + $ihw * 0.85), [float]($cy + $iMid),
        [float]($cx + $ihw * 0.45), [float]($cy + $iBot * 0.65),
        [float]($cx + $Size * 0.015),[float]($cy + $iBot * 0.95),
        [float]$cx,                  [float]($cy + $iBot)
    )
    $innerShield.AddBezier(
        [float]$cx,                  [float]($cy + $iBot),
        [float]($cx - $Size * 0.015),[float]($cy + $iBot * 0.95),
        [float]($cx - $ihw * 0.45), [float]($cy + $iBot * 0.65),
        [float]($cx - $ihw * 0.85), [float]($cy + $iMid)
    )
    $innerShield.AddBezier(
        [float]($cx - $ihw * 0.85), [float]($cy + $iMid),
        [float]($cx - $ihw * 0.95), [float]($cy + $iMid * 0.5),
        [float]($cx - $ihw * 0.97), [float]($cy - $Size * 0.01),
        [float]($cx - $ihw),        [float]($cy - $iTop + $Size * 0.03)
    )
    $innerShield.CloseFigure()

    $innerPen = New-Object System.Drawing.Pen(
        [System.Drawing.Color]::FromArgb(200, 255, 255, 255), [float]($Size * 0.012))
    $innerPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($innerPen, $innerShield)

    $g.Dispose()
    return $bmp
}

# ---------------------------------------------------------------------------
# Write multi-resolution .ICO (PNG-in-ICO, Vista+ compatible)
# ---------------------------------------------------------------------------
function Save-Ico {
    param([System.Drawing.Bitmap[]]$Bitmaps, [string]$Path)

    # Encode each bitmap as PNG bytes
    $pngChunks = foreach ($bmp in $Bitmaps) {
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        , $ms.ToArray()
        $ms.Dispose()
    }

    $count = $Bitmaps.Length
    $ms    = New-Object System.IO.MemoryStream
    $bw    = New-Object System.IO.BinaryWriter($ms)

    # ICO header
    $bw.Write([uint16]0)       # reserved
    $bw.Write([uint16]1)       # type = ICO
    $bw.Write([uint16]$count)

    # Directory entries (16 bytes each)
    $dataOffset = 6 + ($count * 16)
    for ($i = 0; $i -lt $count; $i++) {
        $bmp   = $Bitmaps[$i]
        $wByte = if ($bmp.Width  -ge 256) { [byte]0 } else { [byte]$bmp.Width  }
        $hByte = if ($bmp.Height -ge 256) { [byte]0 } else { [byte]$bmp.Height }
        $bw.Write($wByte)
        $bw.Write($hByte)
        $bw.Write([byte]0)          # color count
        $bw.Write([byte]0)          # reserved
        $bw.Write([uint16]1)        # color planes
        $bw.Write([uint16]32)       # bits per pixel
        $bw.Write([uint32]$pngChunks[$i].Length)
        $bw.Write([uint32]$dataOffset)
        $dataOffset += $pngChunks[$i].Length
    }

    # Image data
    foreach ($chunk in $pngChunks) { $bw.Write($chunk) }

    $bw.Flush()
    [System.IO.File]::WriteAllBytes($Path, $ms.ToArray())
    $bw.Dispose()
    $ms.Dispose()
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
$root       = Split-Path -Parent $MyInvocation.MyCommand.Path
$assetsDir  = Join-Path $root 'Assets'
$null       = New-Item -ItemType Directory -Force -Path $assetsDir

$sizes   = @(256, 64, 48, 32, 16)
$bitmaps = $sizes | ForEach-Object { New-IconBitmap -Size $_ }

$icoPath = Join-Path $assetsDir 'AdminToolbox.ico'
Save-Ico -Bitmaps $bitmaps -Path $icoPath

foreach ($b in $bitmaps) { $b.Dispose() }

Write-Host "Icon written to: $icoPath" -ForegroundColor Green
