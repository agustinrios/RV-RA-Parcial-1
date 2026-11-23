# Verifica el tamano REAL con el que una imagen queda dibujada dentro de un PDF.
#
# En PDF las imagenes se dibujan siempre en un cuadrado unitario y es la matriz del operador
# "cm" la que le da su tamano final, en puntos (1 pt = 1/72 pulgada). Asi que midiendo esa
# matriz se sabe cuantos milimetros va a medir la imagen sobre el papel, sin depender de lo
# que diga el visor ni de como se vea en pantalla.

param([Parameter(Mandatory = $true)][string]$Path)

$bytes = [System.IO.File]::ReadAllBytes($Path)
$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$texto = $latin1.GetString($bytes)

$inflado = New-Object System.Text.StringBuilder
$pos = 0

while ($true) {
    $inicio = $texto.IndexOf('stream', $pos)
    if ($inicio -lt 0) { break }
    if ($inicio -ge 3 -and $texto.Substring($inicio - 3, 3) -eq 'end') { $pos = $inicio + 6; continue }

    $datos = $inicio + 6
    if ($texto[$datos] -eq "`r") { $datos++ }
    if ($texto[$datos] -eq "`n") { $datos++ }

    $fin = $texto.IndexOf('endstream', $datos)
    if ($fin -lt 0) { break }

    $largo = $fin - $datos
    if ($largo -gt 2) {
        try {
            $crudo = New-Object byte[] ($largo - 2)
            [Array]::Copy($bytes, $datos + 2, $crudo, 0, $largo - 2)
            $entrada = New-Object System.IO.MemoryStream($crudo, $false)
            $deflate = New-Object System.IO.Compression.DeflateStream($entrada, [System.IO.Compression.CompressionMode]::Decompress)
            $salida = New-Object System.IO.MemoryStream
            $deflate.CopyTo($salida)
            $deflate.Dispose()
            [void]$inflado.Append($latin1.GetString($salida.ToArray()))
        } catch { }
    }
    $pos = $fin + 9
}

$todo = $texto + $inflado.ToString()

# Tamano de pagina
$caja = [regex]::Match($todo, '/MediaBox\s*\[\s*([\d.\-]+)\s+([\d.\-]+)\s+([\d.\-]+)\s+([\d.\-]+)\s*\]')
if ($caja.Success) {
    $ancho = [double]$caja.Groups[3].Value - [double]$caja.Groups[1].Value
    $alto = [double]$caja.Groups[4].Value - [double]$caja.Groups[2].Value
    Write-Output ("PAGINA = {0:N1} x {1:N1} mm   (A4 son 210,0 x 297,0)" -f ($ancho / 72 * 25.4), ($alto / 72 * 25.4))
}

# Transformacion de pagina. Chrome rasteriza a 300 dpi y despues encoge todo con un
# "cm" de 72/300 = 0.24 al principio del content stream. Sin tenerla en cuenta, cualquier
# medida que se lea despues sale multiplicada por 4,1666.
$escalaPagina = 1.0
$tp = [regex]::Match($todo, '(?<![\d.])(\.\d+|\d+\.\d+)\s+0\s+0\s+-?(\.\d+|\d+\.\d+)\s+0\s+[\d.]+\s+cm')
if ($tp.Success) {
    $escalaPagina = [double]("0" + $tp.Groups[1].Value)
    Write-Output ("TRANSFORMACION DE PAGINA = {0:N4}  (1 pt dibujado = {0:N4} pt reales)" -f $escalaPagina)
}

# Matrices de dibujo de imagenes: "<w> 0 0 <h> <x> <y> cm" seguido de un "/Nombre Do"
# Se aceptan valores negativos: una imagen volteada se dibuja con la escala Y en negativo.
$dibujos = [regex]::Matches($todo, '([\d.\-]+)\s+[\d.\-]+\s+[\d.\-]+\s+([\d.\-]+)\s+[\d.\-]+\s+[\d.\-]+\s+cm(?:(?!\scm\b|\sre\b)[\s\S]){0,120}?/[A-Za-z0-9]+\s+Do')

if ($dibujos.Count -eq 0) {
    Write-Output "No encontre ninguna imagen dibujada."
    return
}

$n = 0
foreach ($d in $dibujos) {
    $n++
    $wmm = [math]::Abs([double]$d.Groups[1].Value) * $escalaPagina / 72 * 25.4
    $hmm = [math]::Abs([double]$d.Groups[2].Value) * $escalaPagina / 72 * 25.4
    Write-Output ("IMAGEN {0} = {1:N2} x {2:N2} mm" -f $n, $wmm, $hmm)
}
