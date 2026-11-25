$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class RingIconHandles { [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon); }'
$assetDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
New-Item -ItemType Directory -Path $assetDir -Force | Out-Null
$bitmap = New-Object Drawing.Bitmap 64,64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([Drawing.Color]::Transparent)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$background = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(24,24,24))
$track = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(82,82,82)),3
$white = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(242,242,242)),3
$white.StartCap = [Drawing.Drawing2D.LineCap]::Round
$white.EndCap = [Drawing.Drawing2D.LineCap]::Round
$graphics.FillEllipse($background,0,0,64,64)
$graphics.DrawEllipse($track,5,17,30,30)
$graphics.DrawEllipse($track,29,17,30,30)
$graphics.DrawArc($white,5,17,30,30,-90,264)
$graphics.DrawArc($white,29,17,30,30,-90,112)
$handle = $bitmap.GetHicon()
$icon = [Drawing.Icon]::FromHandle($handle)
$file = [IO.File]::Create((Join-Path $assetDir 'CodexRings.ico'))
try { $icon.Save($file) }
finally {
    $file.Dispose(); $icon.Dispose(); [RingIconHandles]::DestroyIcon($handle) | Out-Null
    $white.Dispose(); $track.Dispose(); $background.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
