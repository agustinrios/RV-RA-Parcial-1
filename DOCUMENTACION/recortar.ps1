# Recorta una region cuadrada de una imagen y la guarda como JPG de alta calidad.
# Se usa para probar encuadres y medir con arcoreimg cual rastrea mejor.

param(
    [Parameter(Mandatory = $true)][string]$Origen,
    [Parameter(Mandatory = $true)][string]$Destino,
    [Parameter(Mandatory = $true)][int]$X,
    [Parameter(Mandatory = $true)][int]$Y,
    [Parameter(Mandatory = $true)][int]$Lado
)

Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Image]::FromFile($Origen)
$recorte = New-Object System.Drawing.Bitmap($Lado, $Lado)
$g = [System.Drawing.Graphics]::FromImage($recorte)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$destinoRect = New-Object System.Drawing.Rectangle(0, 0, $Lado, $Lado)
$origenRect = New-Object System.Drawing.Rectangle($X, $Y, $Lado, $Lado)
$g.DrawImage($img, $destinoRect, $origenRect, [System.Drawing.GraphicsUnit]::Pixel)

$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
    Where-Object { $_.MimeType -eq 'image/jpeg' }
$params = New-Object System.Drawing.Imaging.EncoderParameters(1)
$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
    [System.Drawing.Imaging.Encoder]::Quality, 95L)

$recorte.Save($Destino, $codec, $params)

$g.Dispose(); $recorte.Dispose(); $img.Dispose()
Write-Output ("{0}  ({1}x{1} desde {2},{3})" -f (Split-Path $Destino -Leaf), $Lado, $X, $Y)
