# Imprime el comienzo del content stream de un PDF, para ver la transformacion de pagina
# que el generador aplica antes de dibujar nada.

param([Parameter(Mandatory = $true)][string]$Path)

$bytes = [System.IO.File]::ReadAllBytes($Path)
$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$texto = $latin1.GetString($bytes)
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

            $contenido = $latin1.GetString($salida.ToArray())
            if ($contenido -match '\sDo\b') {
                $largoMuestra = [math]::Min(220, $contenido.Length)
                Write-Output ("--- content stream con imagen (" + $contenido.Length + " bytes) ---")
                Write-Output $contenido.Substring(0, $largoMuestra).Replace("`n", " ")
                return
            }
        } catch { }
    }
    $pos = $fin + 9
}

Write-Output "No encontre un content stream con imagenes."
