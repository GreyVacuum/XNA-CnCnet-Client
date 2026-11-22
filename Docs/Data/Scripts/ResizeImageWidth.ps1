param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [Parameter(Mandatory = $true)][int]$Width,
    [int]$Height = 0,
    [ValidateSet('Bicubic', 'NearestNeighbor')][string]$Mode = 'Bicubic',
    [switch]$OnlyIfDifferent
)

# ===========================================================================
#  ResizeImageWidth.ps1 - resize a PNG to an exact pixel size.
#
#  Usage:
#    powershell -NoProfile -ExecutionPolicy Bypass -File ResizeImageWidth.ps1 ^
#        -InputPath in.png -OutputPath out.png -Width 78 [-Height 23] ^
#        [-Mode Bicubic|NearestNeighbor] [-OnlyIfDifferent]
#
#  - Height 0 (default): keep the source height (23 for tab art).
#  - OnlyIfDifferent: when the target is already the requested size, skip
#    writing and exit with code 2.
#  - The output is always saved as 32-bit ARGB PNG.
#  - When InputPath and OutputPath are the same file, it is rewritten in place.
#
#  Exit codes:
#    0 = image written (resized or copied)
#    1 = failure
#    2 = skipped, image already has the requested size
# ===========================================================================

$ErrorActionPreference = 'Stop'

try {
    Add-Type -AssemblyName System.Drawing

    $inFull = [System.IO.Path]::GetFullPath($InputPath)
    $outFull = [System.IO.Path]::GetFullPath($OutputPath)

    if (-not (Test-Path -LiteralPath $inFull)) { exit 1 }
    if ($Width -le 0) { exit 1 }

    $source = [System.Drawing.Image]::FromFile($inFull)
    $targetHeight = if ($Height -gt 0) { $Height } else { $source.Height }

    try {
        if ($source.Width -eq $Width -and $source.Height -eq $targetHeight) {
            if ($inFull -ne $outFull) {
                Copy-Item -LiteralPath $inFull -Destination $outFull -Force
            }
            exit 2
        }

        $bitmap = New-Object System.Drawing.Bitmap($Width, $targetHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.InterpolationMode = if ($Mode -eq 'NearestNeighbor') {
                [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            } else {
                [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            }

            $graphics.DrawImage($source, (New-Object System.Drawing.Rectangle(0, 0, $Width, $targetHeight)))
        } finally {
            $graphics.Dispose()
        }

        $tempPath = "$outFull.tmp.png"

        try {
            $bitmap.Save($tempPath, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $bitmap.Dispose()
        }
    } finally {
        $source.Dispose()
    }

    Move-Item -LiteralPath $tempPath -Destination $outFull -Force
    exit 0
} catch {
    exit 1
}
