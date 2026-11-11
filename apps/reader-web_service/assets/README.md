# Brand assets (source files)

Master artwork that is **not** served to the browser. Nothing in this folder is
copied into the Docker image — the Dockerfile only copies `src/`.

| File | Size | Purpose |
| --- | --- | --- |
| `logo-master.png` | 1254 x 1254 | Full-resolution Happy Headlines logo. Derive any new size from this file. |

## Derived files

`src/HappyHeadlinesPages.web/wwwroot/images/logo.png` (512 x 512) is generated
from `logo-master.png`. It is downscaled because it renders at a maximum of
200px in the navigation drawer, so the master's 1254px was roughly 1.1 MB of
wasted download on every page load.

To regenerate it at a different size, run from this folder:

```powershell
Add-Type -AssemblyName System.Drawing
$size = 512
$src = [System.Drawing.Image]::FromFile("$PWD\logo-master.png")
$bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($src, 0, 0, $size, $size)
$g.Dispose(); $src.Dispose()
$bmp.Save("$PWD\..\src\HappyHeadlinesPages.web\wwwroot\images\logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
```

Keep the served file at roughly 2x its largest on-screen size so it stays sharp
on high-DPI screens.
