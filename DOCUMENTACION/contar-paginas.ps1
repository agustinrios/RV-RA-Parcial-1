# Cuenta las paginas de un PDF y lista sus fuentes, sin dependencias externas.
# Los PDF de Chrome guardan los objetos dentro de streams comprimidos con Flate,
# asi que hay que inflarlos todos y recien ahi contar los objetos /Type /Page.

param([Parameter(Mandatory = $true)][string]$Path)

$bytes = [System.IO.File]::ReadAllBytes($Path)
$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$texto = $latin1.GetString($bytes)

$inflado = New-Object System.Text.StringBuilder
$pos = 0

while ($true) {
    $inicio = $texto.IndexOf('stream', $pos)
    if ($inicio -lt 0) { break }

    # "endstream" tambien contiene "stream": hay que descartarlo.
    if ($inicio -ge 3 -and $texto.Substring($inicio - 3, 3) -eq 'end') {
        $pos = $inicio + 6
        continue
    }

    $datos = $inicio + 6
    if ($texto[$datos] -eq "`r") { $datos++ }
    if ($texto[$datos] -eq "`n") { $datos++ }

    $fin = $texto.IndexOf('endstream', $datos)
    if ($fin -lt 0) { break }

    $largo = $fin - $datos

    # Se saltean los 2 bytes de cabecera zlib: DeflateStream espera deflate crudo.
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
        } catch {
            # Stream que no es Flate (una imagen JPEG, una fuente): no importa para el conteo.
        }
    }

    $pos = $fin + 9
}

$todo = $texto + $inflado.ToString()

# /Type /Page, pero no /Type /Pages (que es el nodo del arbol, no una pagina).
$paginas = ([regex]::Matches($todo, '/Type\s*/Page(?![s/A-Za-z])')).Count
Write-Output ("PAGINAS=" + $paginas)

$fuentes = [regex]::Matches($todo, '/BaseFont\s*/([A-Za-z0-9+\-,]+)') |
    ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
if ($fuentes) { Write-Output ("FUENTES=" + ($fuentes -join ', ')) }

$imagenes = ([regex]::Matches($todo, '/Subtype\s*/Image')).Count
Write-Output ("IMAGENES=" + $imagenes)
