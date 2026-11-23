using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Genera por codigo las tres postales marcadoras del proyecto y arma con ellas la
/// Reference Image Library.
///
/// Por que las imagenes se generan y no se descargan:
///   - No hay licencias que atribuir: el marcador es obra propia, igual que los modelos.
///   - Se puede DISENAR para el tracking. ARCore busca puntos caracteristicos, y lo que
///     los da es detalle denso, contraste alto en escala de grises, cero patrones repetidos
///     y una silueta asimetrica. Las tres postales cumplen las cuatro condiciones a proposito,
///     y eso es exactamente lo que el GDD tiene que justificar.
///   - Son reproducibles: la semilla es fija, asi que el resultado es siempre el mismo.
///
/// Menu: Patrimonio AR / 1
/// </summary>
public static class GeneradorPostales
{
    const string kCarpetaImg      = "Assets/Img";
    const string kCarpetaRecursos = "Assets/Recursos";
    const string kRutaLibrary     = "Assets/Recursos/PatrimonioLibrary.asset";

    // Ancho de 15 cm. El alto sale de la proporcion real de cada archivo: con las cartas
    // de 1228 x 1388 px, 16,95 cm. Es la medida que se declara en la library y la que
    // despues se usa para escalar los dioramas.
    public const float kAnchoPostal = 0.15f;
    public const float kAltoPostal  = 0.10f;

    const int kAncho = 1200;
    const int kAlto  = 800;

    // ------------------------------------------------------------------ datos de cada postal
    class Postal
    {
        public string imagen;      // nombre de la entrada en la library (la clave del sistema)
        public string titulo;
        public string provincia;
        public int semilla;

        // Archivos propios a usar como marcador, en orden de preferencia. Si alguno existe en
        // Assets/Img se usa ese y no se genera nada. El nombre del archivo NO tiene por que
        // coincidir con el nombre de la entrada en la library: la clave es 'imagen'.
        public string[] archivos;

        public Color32 fondoOscuro;
        public Color32 fondoMedio;
        public Color32 fondoClaro;
        public Color32 acento;
        public Color32 silueta;
        public bool siluetaClara;  // true: la silueta es clara sobre fondo oscuro
    }

    static List<Postal> Postales()
    {
        return new List<Postal>
        {
            new Postal
            {
                imagen = "Obelisco", titulo = "OBELISCO", provincia = "BUENOS AIRES",
                semilla = 1936,
                archivos = new[] { "Carta_Obelisco.png", "Obelisco.jpg", "Obelisco.jpeg" },
                fondoOscuro = new Color32( 18,  32,  58, 255),
                fondoMedio  = new Color32( 92, 148, 196, 255),
                fondoClaro  = new Color32(238, 242, 246, 255),
                acento      = new Color32(246, 196,  62, 255),
                silueta     = new Color32( 10,  16,  28, 255),
                siluetaClara = false
            },
            new Postal
            {
                imagen = "Cataratas", titulo = "CATARATAS", provincia = "MISIONES",
                semilla = 1984,
                archivos = new[] { "Carta_Cataratas.png", "Cataratas.jpg", "Cataratas.jpeg" },
                fondoOscuro = new Color32( 14,  40,  28, 255),
                fondoMedio  = new Color32( 62, 130,  86, 255),
                fondoClaro  = new Color32(236, 244, 238, 255),
                acento      = new Color32(240, 142,  52, 255),
                silueta     = new Color32(  8,  22,  16, 255),
                siluetaClara = false
            },
            new Postal
            {
                imagen = "Perito_Moreno", titulo = "PERITO MORENO", provincia = "SANTA CRUZ",
                semilla = 1937,
                archivos = new[] { "Carta_Perito_Moreno.png", "Glaciar.jpg", "Glaciar.jpeg" },
                fondoOscuro = new Color32( 16,  28,  44, 255),
                fondoMedio  = new Color32( 78, 134, 166, 255),
                fondoClaro  = new Color32(240, 248, 252, 255),
                acento      = new Color32( 72, 214, 226, 255),
                silueta     = new Color32(244, 250, 254, 255),
                siluetaClara = true
            }
        };
    }

    // ================================================================ MENU
    [MenuItem("Patrimonio AR/1 - Generar postales y library", false, 20)]
    public static void Generar()
    {
        AsegurarCarpeta(kCarpetaImg, "Img");
        AsegurarCarpeta(kCarpetaRecursos, "Recursos");

        var rutas = new List<string>();
        var nombres = new List<string>();
        var tamanos = new List<Vector2>();
        var detalle = new System.Text.StringBuilder();

        foreach (var postal in Postales())
        {
            // Si hay una imagen propia en Assets/Img se usa esa. Solo si no hay ninguna se
            // dibuja la postal por codigo.
            string ruta = BuscarImagenPropia(postal);
            bool propia = ruta != null;

            if (!propia) ruta = DibujarPostal(postal);

            ConfigurarImportador(ruta);

            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            Vector2 tamano = TamanoFisico(textura);

            rutas.Add(ruta);
            nombres.Add(postal.imagen);
            tamanos.Add(tamano);

            string linea = postal.imagen + ": " + System.IO.Path.GetFileName(ruta) +
                           "  (" + Mathf.RoundToInt(tamano.x * 100f) + " x " +
                           Mathf.RoundToInt(tamano.y * 100f) + " cm)" +
                           (propia ? "  [imagen propia]" : "  [generada por codigo]");

            detalle.AppendLine(linea);
            Debug.Log("[Patrimonio AR] " + linea);
        }

        ArmarLibrary(nombres, rutas, tamanos);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Patrimonio AR",
            "Library armada en " + kRutaLibrary + "\n\n" + detalle +
            "\nEl alto se calcula con la proporcion real de cada imagen: declarar una " +
            "proporcion que no es la de la foto hace que el sistema estime mal la distancia.\n\n" +
            "Abri la library y fijate que las 3 tengan buena calidad de tracking.\n\n" +
            "Siguiente paso: Patrimonio AR > 2 - Generar dioramas y armar escena.", "Listo");
    }

    /// <summary>Devuelve la primera imagen propia que exista en Assets/Img, o null.</summary>
    static string BuscarImagenPropia(Postal p)
    {
        if (p.archivos == null) return null;

        foreach (var archivo in p.archivos)
        {
            string ruta = kCarpetaImg + "/" + archivo;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(ruta) != null)
                return ruta;
        }

        return null;
    }

    /// <summary>
    /// Ancho fijo de 15 cm y alto segun la proporcion REAL de la
    /// imagen. Si se declara una proporcion que no es la de la foto, el sistema calcula mal
    /// la distancia a la tarjeta y el diorama entra torcido respecto de ella.
    /// </summary>
    static Vector2 TamanoFisico(Texture2D textura)
    {
        if (textura == null || textura.width <= 0)
            return new Vector2(kAnchoPostal, kAltoPostal);

        return new Vector2(kAnchoPostal, kAnchoPostal * textura.height / (float)textura.width);
    }

    static string DibujarPostal(Postal postal)
    {
        var pixeles = Dibujar(postal);

        var textura = new Texture2D(kAncho, kAlto, TextureFormat.RGBA32, false, false);
        textura.SetPixels32(pixeles);
        textura.Apply();

        string ruta = kCarpetaImg + "/" + postal.imagen + ".png";
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        return ruta;
    }

    // ================================================================ library
    /// <summary>
    /// Arma la Reference Image Library por codigo. El campo Name es la clave de todo el
    /// sistema: el spawner busca el prefab por ese nombre. Escribirlo a mano es el error
    /// mas facil de cometer y el mas dificil de encontrar, porque no tira ningun error:
    /// simplemente no aparece nada. Generandolo, no puede estar mal.
    /// </summary>
    static void ArmarLibrary(List<string> nombres, List<string> rutas, List<Vector2> tamanos)
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(kRutaLibrary);
        bool nueva = library == null;

        if (nueva)
        {
            library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(library, kRutaLibrary);
        }

        for (int i = 0; i < nombres.Count; i++)
        {
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(rutas[i]);
            if (textura == null)
            {
                Debug.LogError("[Patrimonio AR] No pude cargar la textura " + rutas[i]);
                continue;
            }

            int indice = IndiceDe(library, nombres[i]);
            if (indice < 0)
            {
                library.Add();
                indice = library.count - 1;
            }

            library.SetTexture(indice, textura, true);
            library.SetName(indice, nombres[i]);
            library.SetSpecifySize(indice, true);
            library.SetSize(indice, tamanos[i]);
        }

        EditorUtility.SetDirty(library);
        Debug.Log("[Patrimonio AR] Library con " + library.count + " imagenes: " + kRutaLibrary);
    }

    static int IndiceDe(XRReferenceImageLibrary library, string nombre)
    {
        for (int i = 0; i < library.count; i++)
            if (library[i].name == nombre) return i;
        return -1;
    }

    static void ConfigurarImportador(string ruta)
    {
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) return;

        importador.textureType = TextureImporterType.Default;
        importador.npotScale = TextureImporterNPOTScale.None;
        importador.mipmapEnabled = false;
        importador.sRGBTexture = true;
        importador.isReadable = true;                 // AR Foundation necesita leer los pixeles
        importador.maxTextureSize = 2048;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.filterMode = FilterMode.Bilinear;

        importador.SaveAndReimport();
    }

    // ================================================================ dibujo de la postal
    static Color32[] Dibujar(Postal p)
    {
        var buffer = new Color32[kAncho * kAlto];
        var rnd = new System.Random(p.semilla);

        // --- marco asimetrico: banda gruesa a la izquierda y abajo, fina arriba y a la derecha.
        // La asimetria es lo que le permite a ARCore resolver la ORIENTACION de la postal
        // sin ambiguedad.
        const int bandaIzq = 62;
        const int bandaAbajo = 140;
        const int bandaArriba = 72;
        const int bandaDer = 22;

        Rellenar(buffer, p.fondoOscuro);

        // --- area de imagen
        int x0 = bandaIzq, y0 = bandaArriba;
        int x1 = kAncho - bandaDer, y1 = kAlto - bandaAbajo;
        Rect(buffer, x0, y0, x1 - x0, y1 - y0, p.siluetaClara ? p.fondoOscuro : p.fondoClaro);

        // --- mosaico de fondo: detalle denso y no repetitivo, en tres tonos de contraste alto.
        // Es lo que le da a ARCore la cantidad de puntos caracteristicos que necesita.
        Mosaico(buffer, rnd, p, x0, y0, x1, y1);

        // --- silueta del lugar, corrida a la derecha del centro (otra vez: asimetria)
        switch (p.imagen)
        {
            case "Obelisco":      SiluetaObelisco(buffer, p, x0, y0, x1, y1); break;
            case "Cataratas":     SiluetaCataratas(buffer, p, x0, y0, x1, y1); break;
            case "Perito_Moreno": SiluetaGlaciar(buffer, p, x0, y0, x1, y1); break;
        }

        // --- banda izquierda: marcas irregulares, distintas en cada postal
        for (int i = 0; i < 9; i++)
        {
            int alto = 18 + rnd.Next(40);
            int y = 90 + i * 72 + rnd.Next(14);
            int ancho = 14 + rnd.Next(30);
            Rect(buffer, 10, y, ancho, alto, i % 3 == 0 ? p.acento : p.fondoClaro);
        }

        // --- sello arriba a la derecha
        Sello(buffer, p, rnd, kAncho - 212, 96);

        // --- textos
        Texto(buffer, "PATRIMONIO ARGENTINO", 84, 24, 3, p.fondoClaro);
        Texto(buffer, p.titulo, 70, kAlto - 118, 9, p.fondoClaro);
        Texto(buffer, p.provincia, 72, kAlto - 46, 4, p.acento);

        // --- linea de acento entre el titulo y la provincia
        Rect(buffer, 70, kAlto - 58, 560, 5, p.acento);

        return buffer;
    }

    /// <summary>
    /// Mosaico pseudoaleatorio de rectangulos y circulos en tres tonos. La semilla es fija,
    /// asi que el patron es siempre el mismo, pero no se repite dentro de la imagen: eso
    /// ultimo es lo que ARCore penaliza (un patron que se repite le da puntos ambiguos).
    /// </summary>
    static void Mosaico(Color32[] buffer, System.Random rnd, Postal p, int x0, int y0, int x1, int y1)
    {
        int ancho = x1 - x0, alto = y1 - y0;

        // Tres pasadas de escalas distintas: manchas grandes, medianas y detalle fino.
        int[] cantidades = { 26, 70, 190 };
        int[] tamanos    = { 150, 72, 26 };

        for (int pasada = 0; pasada < 3; pasada++)
        {
            for (int i = 0; i < cantidades[pasada]; i++)
            {
                int w = 8 + rnd.Next(tamanos[pasada]);
                int h = 8 + rnd.Next(tamanos[pasada]);
                int x = x0 + rnd.Next(Mathf.Max(1, ancho - w));
                int y = y0 + rnd.Next(Mathf.Max(1, alto - h));

                int tono = rnd.Next(100);
                Color32 color = tono < 42 ? p.fondoOscuro
                              : tono < 78 ? p.fondoMedio
                              : tono < 92 ? p.fondoClaro
                                          : p.acento;

                if (rnd.Next(100) < 70)
                    Rect(buffer, x, y, w, h, color);
                else
                    Circulo(buffer, x + w / 2, y + h / 2, Mathf.Min(w, h) / 2, color);
            }
        }
    }

    static void Sello(Color32[] buffer, Postal p, System.Random rnd, int x, int y)
    {
        const int lado = 150;

        Rect(buffer, x - 6, y - 6, lado + 12, lado + 12, p.fondoClaro);
        Rect(buffer, x, y, lado, lado, p.fondoOscuro);

        // dentado del sello
        for (int i = 0; i < 10; i++)
        {
            int d = 8;
            Circulo(buffer, x + i * (lado / 9), y - 6, d, p.fondoOscuro);
            Circulo(buffer, x + i * (lado / 9), y + lado + 6, d, p.fondoOscuro);
            Circulo(buffer, x - 6, y + i * (lado / 9), d, p.fondoOscuro);
            Circulo(buffer, x + lado + 6, y + i * (lado / 9), d, p.fondoOscuro);
        }

        // contenido interno distinto en cada postal
        for (int i = 0; i < 7; i++)
        {
            int m = 12 + i * 9;
            Color32 c = i % 2 == 0 ? p.acento : p.fondoClaro;
            Rect(buffer, x + m, y + m + rnd.Next(6), lado - m * 2, 5, c);
        }

        Texto(buffer, "AR", x + 46, y + 96, 5, p.acento);

        // matasellos: dos diagonales, que rompen cualquier simetria que quedara
        Linea(buffer, x - 10, y + 30, x + lado + 10, y + 86, 7, p.fondoClaro);
        Linea(buffer, x - 10, y + 54, x + lado + 10, y + 112, 5, p.fondoClaro);
    }

    // ================================================================ siluetas
    static void SiluetaObelisco(Color32[] buffer, Postal p, int x0, int y0, int x1, int y1)
    {
        int cx = x0 + (int)((x1 - x0) * 0.62f);   // corrido a la derecha
        int baseY = y1 - 40;
        int topeY = y0 + 40;
        int altura = baseY - topeY;

        int mediaBase = 62, mediaTope = 26;
        int yPiramide = topeY + (int)(altura * 0.13f);

        // fuste troncopiramidal
        Poligono(buffer, new[]
        {
            new Vector2(cx - mediaBase, baseY),
            new Vector2(cx + mediaBase, baseY),
            new Vector2(cx + mediaTope, yPiramide),
            new Vector2(cx - mediaTope, yPiramide)
        }, p.silueta);

        // piramidion
        Poligono(buffer, new[]
        {
            new Vector2(cx - mediaTope, yPiramide),
            new Vector2(cx + mediaTope, yPiramide),
            new Vector2(cx, topeY)
        }, p.silueta);

        // plaza
        Rect(buffer, cx - 210, baseY, 420, 26, p.silueta);
        Rect(buffer, cx - 150, baseY - 14, 300, 16, p.silueta);

        // figura humana a escala (1,75 m contra 67,5 m): el concepto del proyecto, ya en la postal
        int altoHumano = Mathf.Max(8, (int)(altura * 1.75f / 67.5f));
        Rect(buffer, cx - 250, baseY - altoHumano, Mathf.Max(3, altoHumano / 3), altoHumano, p.acento);
    }

    static void SiluetaCataratas(Color32[] buffer, Postal p, int x0, int y0, int x1, int y1)
    {
        int bordeY = y0 + (int)((y1 - y0) * 0.42f);
        int aguaY  = y1 - 70;

        // bloque de roca izquierdo
        Poligono(buffer, new[]
        {
            new Vector2(x0 + 60,  bordeY),
            new Vector2(x0 + 330, bordeY - 26),
            new Vector2(x0 + 360, aguaY + 40),
            new Vector2(x0 + 40,  aguaY + 60)
        }, p.silueta);

        // bloque de roca derecho, mas alto: escalon de la Garganta del Diablo
        Poligono(buffer, new[]
        {
            new Vector2(x0 + 560, bordeY - 70),
            new Vector2(x1 - 40,  bordeY - 44),
            new Vector2(x1 - 30,  aguaY + 50),
            new Vector2(x0 + 540, aguaY + 30)
        }, p.silueta);

        // fondo oscuro de la garganta, para que el agua clara tenga contra que contrastar
        Rect(buffer, x0 + 340, bordeY - 34, 220, aguaY - bordeY + 74, p.silueta);

        // los dos saltos de agua, en claro contra la roca oscura
        Rect(buffer, x0 + 360, bordeY - 20, 180, aguaY - bordeY + 40, p.fondoClaro);
        Rect(buffer, x0 + 420, bordeY - 10,  58, aguaY - bordeY + 60, p.acento);

        // espuma al pie
        for (int i = 0; i < 16; i++)
        {
            int r = 14 + (i * 7) % 34;
            Circulo(buffer, x0 + 330 + (i * 47) % 300, aguaY + 20 + (i * 13) % 34, r, p.fondoClaro);
        }

        // linea de selva sobre el borde
        for (int i = 0; i < 22; i++)
            Circulo(buffer, x0 + 40 + i * 48, bordeY - 90 - (i % 4) * 16, 30 + (i % 3) * 10, p.silueta);
    }

    static void SiluetaGlaciar(Color32[] buffer, Postal p, int x0, int y0, int x1, int y1)
    {
        int aguaY = y1 - 90;
        int baseY = y0 + (int)((y1 - y0) * 0.30f);

        // frente de hielo: borde superior en zigzag, nunca dos picos iguales
        var puntos = new List<Vector2>();
        int[] alturas = { 0, -54, -18, -78, -30, -96, -42, -66, -12, -58, -26 };

        puntos.Add(new Vector2(x0 + 30, aguaY));
        for (int i = 0; i < alturas.Length; i++)
        {
            float x = x0 + 30 + i * ((x1 - x0 - 60f) / (alturas.Length - 1));
            puntos.Add(new Vector2(x, baseY + alturas[i]));
        }
        puntos.Add(new Vector2(x1 - 30, aguaY));

        Poligono(buffer, puntos.ToArray(), p.silueta);

        // grietas verticales
        for (int i = 0; i < 9; i++)
        {
            int x = x0 + 70 + i * 104;
            Linea(buffer, x, baseY + 10, x + (i % 3 - 1) * 18, aguaY - 10, 6, p.fondoMedio);
        }

        // lago
        Rect(buffer, x0, aguaY, x1 - x0, y1 - aguaY, p.fondoOscuro);

        // tempanos flotando
        for (int i = 0; i < 7; i++)
            Rect(buffer, x0 + 60 + i * 136, aguaY + 18 + (i % 3) * 16, 40 + (i % 4) * 14, 12, p.silueta);

        // lancha con la gente: el dato de escala
        Rect(buffer, x0 + 180, aguaY + 44, 96, 16, p.acento);
        Rect(buffer, x0 + 210, aguaY + 30, 34, 16, p.acento);
    }

    // ================================================================ primitivas de dibujo
    static void Rellenar(Color32[] buffer, Color32 color)
    {
        for (int i = 0; i < buffer.Length; i++) buffer[i] = color;
    }

    /// <summary>
    /// Escribe un pixel. El dibujo se piensa con el origen arriba a la izquierda (como una
    /// hoja), pero las texturas de Unity se guardan de abajo hacia arriba: aca se da vuelta.
    /// </summary>
    static void Pixel(Color32[] buffer, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= kAncho || y >= kAlto) return;
        buffer[(kAlto - 1 - y) * kAncho + x] = color;
    }

    static void Rect(Color32[] buffer, int x, int y, int ancho, int alto, Color32 color)
    {
        for (int j = y; j < y + alto; j++)
            for (int i = x; i < x + ancho; i++)
                Pixel(buffer, i, j, color);
    }

    static void Circulo(Color32[] buffer, int cx, int cy, int radio, Color32 color)
    {
        if (radio <= 0) return;
        int r2 = radio * radio;

        for (int j = -radio; j <= radio; j++)
            for (int i = -radio; i <= radio; i++)
                if (i * i + j * j <= r2)
                    Pixel(buffer, cx + i, cy + j, color);
    }

    static void Linea(Color32[] buffer, int xa, int ya, int xb, int yb, int grosor, Color32 color)
    {
        float largo = Mathf.Max(1f, Vector2.Distance(new Vector2(xa, ya), new Vector2(xb, yb)));
        int pasos = Mathf.CeilToInt(largo);

        for (int s = 0; s <= pasos; s++)
        {
            float t = s / (float)pasos;
            int x = Mathf.RoundToInt(Mathf.Lerp(xa, xb, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(ya, yb, t));
            Circulo(buffer, x, y, grosor / 2, color);
        }
    }

    /// <summary>Relleno de poligono por scanline con regla par-impar.</summary>
    static void Poligono(Color32[] buffer, Vector2[] puntos, Color32 color)
    {
        if (puntos == null || puntos.Length < 3) return;

        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var punto in puntos)
        {
            if (punto.y < minY) minY = punto.y;
            if (punto.y > maxY) maxY = punto.y;
        }

        var cruces = new List<float>();

        for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
        {
            cruces.Clear();
            float escaneo = y + 0.5f;

            for (int i = 0; i < puntos.Length; i++)
            {
                Vector2 a = puntos[i];
                Vector2 b = puntos[(i + 1) % puntos.Length];

                if ((a.y <= escaneo && b.y > escaneo) || (b.y <= escaneo && a.y > escaneo))
                {
                    float t = (escaneo - a.y) / (b.y - a.y);
                    cruces.Add(a.x + t * (b.x - a.x));
                }
            }

            cruces.Sort();

            for (int i = 0; i + 1 < cruces.Count; i += 2)
            {
                int desde = Mathf.RoundToInt(cruces[i]);
                int hasta = Mathf.RoundToInt(cruces[i + 1]);
                for (int x = desde; x <= hasta; x++)
                    Pixel(buffer, x, y, color);
            }
        }
    }

    // ================================================================ tipografia propia 5x7
    // Se dibuja letra por letra con una matriz de 5 columnas por 7 filas. Es la forma de poner
    // texto en la imagen sin depender de ninguna fuente instalada y sin cuestiones de licencia.
    static readonly Dictionary<char, string> kFuente = new Dictionary<char, string>
    {
        { 'A', ".###./#...#/#...#/#####/#...#/#...#/#...#" },
        { 'B', "####./#...#/#...#/####./#...#/#...#/####." },
        { 'C', ".###./#...#/#..../#..../#..../#...#/.###." },
        { 'D', "####./#...#/#...#/#...#/#...#/#...#/####." },
        { 'E', "#####/#..../#..../####./#..../#..../#####" },
        { 'F', "#####/#..../#..../####./#..../#..../#...." },
        { 'G', ".###./#...#/#..../#.###/#...#/#...#/.###." },
        { 'H', "#...#/#...#/#...#/#####/#...#/#...#/#...#" },
        { 'I', "#####/..#../..#../..#../..#../..#../#####" },
        { 'J', "..###/...#./...#./...#./...#./#..#./.##.." },
        { 'K', "#...#/#..#./#.#../##.../#.#../#..#./#...#" },
        { 'L', "#..../#..../#..../#..../#..../#..../#####" },
        { 'M', "#...#/##.##/#.#.#/#...#/#...#/#...#/#...#" },
        { 'N', "#...#/##..#/#.#.#/#..##/#...#/#...#/#...#" },
        { 'O', ".###./#...#/#...#/#...#/#...#/#...#/.###." },
        { 'P', "####./#...#/#...#/####./#..../#..../#...." },
        { 'Q', ".###./#...#/#...#/#...#/#.#.#/#..#./.##.#" },
        { 'R', "####./#...#/#...#/####./#.#../#..#./#...#" },
        { 'S', ".####/#..../#..../.###./....#/....#/####." },
        { 'T', "#####/..#../..#../..#../..#../..#../..#.." },
        { 'U', "#...#/#...#/#...#/#...#/#...#/#...#/.###." },
        { 'V', "#...#/#...#/#...#/#...#/#...#/.#.#./..#.." },
        { 'W', "#...#/#...#/#...#/#.#.#/#.#.#/##.##/#...#" },
        { 'X', "#...#/#...#/.#.#./..#../.#.#./#...#/#...#" },
        { 'Y', "#...#/#...#/.#.#./..#../..#../..#../..#.." },
        { 'Z', "#####/....#/...#./..#../.#.../#..../#####" },
        { '0', ".###./#...#/#..##/#.#.#/##..#/#...#/.###." },
        { '1', "..#../.##../..#../..#../..#../..#../.###." },
        { '2', ".###./#...#/....#/...#./..#../.#.../#####" },
        { '3', "####./....#/....#/.###./....#/....#/####." },
        { '4', "...#./..##./.#.#./#..#./#####/...#./...#." },
        { '5', "#####/#..../####./....#/....#/#...#/.###." },
        { '6', "..##./.#.../#..../####./#...#/#...#/.###." },
        { '7', "#####/....#/...#./..#../.#.../.#.../.#..." },
        { '8', ".###./#...#/#...#/.###./#...#/#...#/.###." },
        { '9', ".###./#...#/#...#/.####/....#/...#./.##.." },
        { ' ', "...../...../...../...../...../...../....." },
        { '-', "...../...../...../#####/...../...../....." },
        { '.', "...../...../...../...../...../..##./..##." },
        { ',', "...../...../...../...../..##./..##./.#..." }
    };

    static void Texto(Color32[] buffer, string contenido, int x, int y, int escala, Color32 color)
    {
        int cursor = x;

        foreach (char bruto in contenido.ToUpperInvariant())
        {
            char c = bruto;
            if (!kFuente.TryGetValue(c, out var glifo)) c = ' ';
            glifo = kFuente[c];

            var filas = glifo.Split('/');
            for (int fila = 0; fila < filas.Length; fila++)
            {
                for (int col = 0; col < filas[fila].Length; col++)
                {
                    if (filas[fila][col] != '#') continue;
                    Rect(buffer, cursor + col * escala, y + fila * escala, escala, escala, color);
                }
            }

            cursor += 6 * escala;   // 5 de ancho + 1 de separacion
        }
    }

    static void AsegurarCarpeta(string ruta, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(ruta))
            AssetDatabase.CreateFolder("Assets", nombre);
    }
}
