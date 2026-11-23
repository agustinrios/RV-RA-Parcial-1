using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Genera los tres dioramas del proyecto, la ficha de datos, el HUD de tracking, y cablea
/// todo en la escena de AR.
///
/// Decision de diseno central: cada lugar se modela en METROS REALES y despues se escala el
/// contenedor entero, con el MISMO factor para los tres (1:400), para que entre en la postal.
/// De ahi sale lo que da sentido al proyecto: las tres alturas son comparables entre si de un
/// vistazo, sin que haya que explicarlas. El dato en metros va escrito en la ficha.
///
/// No hay figuras humanas de referencia: a 1:400 una persona mide 4 mm y no se ve, asi que
/// sumaba geometria y no aportaba la lectura de escala que se buscaba.
///
/// Menu: Patrimonio AR / 2
/// </summary>
public static class GeneradorPatrimonioAR
{
    // ------------------------------------------------------------------ rutas
    const string kCarpetaRaiz       = "Assets/Patrimonio";
    const string kCarpetaMateriales = "Assets/Patrimonio/Materiales";
    const string kCarpetaPrefabs    = "Assets/Patrimonio/Prefabs";
    const string kCarpetaMallas     = "Assets/Patrimonio/Mallas";
    const string kRutaLibrary       = "Assets/Recursos/PatrimonioLibrary.asset";
    const string kRutaEscena        = "Assets/Scenes/SampleScene.unity";

    // Escala UNICA para los tres dioramas. Es la decision que permite compararlos entre si.
    //
    // Antes cada lugar se escalaba para llenar su propia postal, y como la escena del Obelisco
    // mide 60 m de ancho y la seccion de glaciar 120 m, cada tarjeta terminaba con una escala
    // distinta (1:364, 1:667, 1:727). Consecuencia: el Obelisco, de 67,5 m, se veia casi al
    // doble de alto que el frente del Perito Moreno, de 70 m. Justo al reves de la realidad.
    //
    // Con una escala comun, los tres lugares se modelan sobre una seccion de ancho parecido
    // (unos 60 m) para que igual entren sobre la postal.
    const int kDenominadorEscala = 400;
    const float kEscala = 1f / kDenominadorEscala;

    // Las figuras humanas van siempre del mismo naranja saturado: son el termino de comparacion
    // de todo el proyecto y tienen que poder ubicarse de un vistazo contra la piedra, el agua
    // o el hielo. A 1:400 una persona de 1,75 m mide 4,4 mm, asi que ademas lleva un disco a
    // sus pies: no pretende ser parte del lugar, es lo que la vuelve encontrable.

    // ------------------------------------------------------------------ datos
    class Lugar
    {
        public string imagen;        // coincide EXACTO con el Name de la library
        public string titulo;
        public string[] datos;       // tres renglones como maximo: mas no se leen

        public float anchoReal;      // ancho modelado, en metros reales
        public float alturaReal;     // altura del lugar, para el texto de la ficha

        public Color colorA;         // material principal
        public Color colorB;         // material secundario
        public Color acento;         // figura humana, lancha, linea de la ficha
    }

    static List<Lugar> Lugares()
    {
        return new List<Lugar>
        {
            new Lugar
            {
                imagen = "Obelisco",
                titulo = "OBELISCO",
                datos = new[]
                {
                    "Buenos Aires",
                    "67,5 m de altura",
                    "Inaugurado en 1936"
                },
                anchoReal = 60f, alturaReal = 67.5f,
                colorA = new Color(0.93f, 0.92f, 0.88f),   // piedra clara
                colorB = new Color(0.62f, 0.63f, 0.60f),   // granito de la plaza
                acento = new Color(0.98f, 0.56f, 0.16f)
            },
            new Lugar
            {
                imagen = "Cataratas",
                titulo = "CATARATAS DEL IGUAZU",
                datos = new[]
                {
                    "Misiones",
                    "Salto mayor: 80 m",
                    "275 saltos en 2,7 km"
                },
                anchoReal = 60f, alturaReal = 80f,
                colorA = new Color(0.34f, 0.27f, 0.21f),   // basalto
                colorB = new Color(0.18f, 0.42f, 0.24f),   // selva
                acento = new Color(0.98f, 0.76f, 0.20f)
            },
            new Lugar
            {
                imagen = "Perito_Moreno",
                titulo = "GLACIAR PERITO MORENO",
                datos = new[]
                {
                    "Santa Cruz",
                    "Frente de 70 m de alto",
                    "Avanza 2 m por dia"
                },
                anchoReal = 60f, alturaReal = 70f,
                colorA = new Color(0.82f, 0.90f, 0.95f),   // hielo
                colorB = new Color(0.42f, 0.62f, 0.74f),   // hielo en sombra
                acento = new Color(0.98f, 0.42f, 0.22f)
            }
        };
    }

    // ================================================================ MENU 2
    [MenuItem("Patrimonio AR/2 - Generar dioramas y armar escena", false, 30)]
    public static void GenerarTodo()
    {
        // --- escena de AR abierta
        if (SceneManager.GetActiveScene().path != kRutaEscena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(kRutaEscena);
        }

        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(kRutaLibrary);
        if (library == null)
        {
            EditorUtility.DisplayDialog("Falta la library",
                "No encontre " + kRutaLibrary + "\n\n" +
                "Corre primero: Patrimonio AR > 1 - Generar postales y library.", "Cerrar");
            return;
        }

        var fuente = BuscarFuente();
        if (fuente == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "No hay ninguna fuente de TextMeshPro en el proyecto, y es con lo que se arma la " +
                "ficha de datos.\n\nCorre primero: Patrimonio AR > 0 - Configurar proyecto.", "Cerrar");
            return;
        }

        if (!PrepararEscenaAR(library, out string problema))
        {
            EditorUtility.DisplayDialog("Falta el rig de AR", problema, "Cerrar");
            return;
        }

        AsegurarCarpetas();

        var matPanel  = Mat("Mat_FichaPanel", new Color(0.06f, 0.07f, 0.10f), true);
        var matOscuro = Mat("Mat_Oscuro", new Color(0.05f, 0.05f, 0.06f), false);

        var prefabs = new List<GameObject>();
        var nombres = new List<string>();

        foreach (var lugar in Lugares())
        {
            Vector2 postal = TamanoDeLaPostal(library, lugar.imagen);
            if (postal.x <= 0f)
            {
                Debug.LogWarning("[Patrimonio AR] La imagen " + lugar.imagen +
                                 " no esta en la library. La salteo.");
                continue;
            }

            // Misma escala para los tres: lo que cambia entre un lugar y otro es cuanto mide,
            // no cuanto se lo achica.
            float factor = kEscala;
            int escala = kDenominadorEscala;

            float anchoEnLaPostal = lugar.anchoReal * factor;
            if (anchoEnLaPostal > postal.x * 1.3f)
                Debug.LogWarning("[Patrimonio AR] " + lugar.titulo + " ocupa " +
                                 anchoEnLaPostal.ToString("0.000") + " m de ancho sobre una postal de " +
                                 postal.x.ToString("0.000") + " m: sobresale demasiado. Modelá una " +
                                 "seccion mas angosta o bajá la escala.");

            var raiz = new GameObject("Patrimonio_" + lugar.imagen);

            var diorama = new GameObject("Diorama");
            diorama.transform.SetParent(raiz.transform, false);
            diorama.transform.localScale = Vector3.one * factor;

            // 2,5 mm por encima del plano de la postal. La base del diorama es una superficie
            // plana que, apoyada exactamente sobre la imagen, queda coplanar con ella: dos
            // superficies a la misma profundidad hacen que la placa de video no pueda decidir
            // cual va adelante y el modelo vibra y parpadea (z-fighting). Separarlas unos
            // milimetros lo elimina de raiz y no se nota a simple vista.
            diorama.transform.localPosition = new Vector3(0f, 0.0025f, 0f);

            float alturaReal = 0f;
            switch (lugar.imagen)
            {
                case "Obelisco":      alturaReal = ConstruirObelisco(diorama.transform, lugar); break;
                case "Cataratas":     alturaReal = ConstruirCataratas(diorama.transform, lugar); break;
                case "Perito_Moreno": alturaReal = ConstruirGlaciar(diorama.transform, lugar); break;
            }

            // La ficha va arriba del diorama, ya en metros de la postal. Tres renglones:
            // con mas, cada uno queda tan chico que hay que acercar el telefono para leerlo
            // y la ficha se sale del encuadre.
            ConstruirFicha(raiz.transform, lugar, alturaReal * factor, postal, lugar.datos,
                           matPanel, fuente);

            raiz.AddComponent<AnimacionAparicion>();

            string ruta = kCarpetaPrefabs + "/Patrimonio_" + lugar.imagen + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            Object.DestroyImmediate(raiz);

            prefabs.Add(prefab);
            nombres.Add(lugar.imagen);

            Debug.Log("[Patrimonio AR] " + lugar.titulo + ": seccion de " + lugar.anchoReal +
                      " m y " + alturaReal + " m de alto -> " + anchoEnLaPostal.ToString("0.000") +
                      " x " + (alturaReal * factor).ToString("0.000") + " m en la postal (escala 1:" +
                      escala + ")");
        }

        CablearSpawner(nombres, prefabs);
        CrearHud(fuente);

        var escenaActiva = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escenaActiva);
        EditorSceneManager.SaveScene(escenaActiva);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Patrimonio AR",
            "Se generaron " + prefabs.Count + " dioramas, quedaron cableados en el " +
            "ImageTrackingSpawner, se creo el HUD y se guardo la escena.\n\n" +
            "Siguiente paso: Patrimonio AR > 3 - Preparar entorno de XR Simulation.", "Listo");
    }

    // ================================================================ escena de AR
    /// <summary>
    /// Verifica el rig de AR y completa lo que falte. El AR Session y el XR Origin los tiene
    /// que crear el menu GameObject > XR (son dos clicks y es la via oficial); de ahi para
    /// adelante lo cablea este metodo.
    /// </summary>
    static bool PrepararEscenaAR(XRReferenceImageLibrary library, out string problema)
    {
        problema = null;

        var sesion = Object.FindFirstObjectByType<ARSession>();
        var origen = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();

        if (sesion == null || origen == null)
        {
            problema =
                "En la jerarquia de la escena falta el rig de AR. Crealo con click derecho en la " +
                "jerarquia:\n\n" +
                (sesion == null ? "  - XR > AR Session\n" : "") +
                (origen == null ? "  - XR > XR Origin (Mobile AR)\n" : "") +
                "\nDespues borra la Main Camera del template (la camara de AR viene dentro del " +
                "XR Origin) y volve a correr este menu.";
            return false;
        }

        // La Main Camera del template sobra: la camara de AR es la del XR Origin.
        foreach (var camara in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (camara == null) continue;
            if (camara.transform.IsChildOf(origen.transform)) continue;
            if (camara.GetComponent<ARCameraManager>() != null) continue;

            Debug.Log("[Patrimonio AR] Borre la camara '" + camara.name +
                      "' del template: la camara de AR es la del XR Origin.");
            Object.DestroyImmediate(camara.gameObject);
        }

        // El tracking de imagenes y el spawner van en el XR Origin.
        var manager = origen.GetComponent<ARTrackedImageManager>();
        if (manager == null)
        {
            manager = origen.gameObject.AddComponent<ARTrackedImageManager>();
            Debug.Log("[Patrimonio AR] Agregue el ARTrackedImageManager al XR Origin.");
        }

        var so = new SerializedObject(manager);

        var propLibrary = so.FindProperty("m_SerializedLibrary");
        if (propLibrary != null) propLibrary.objectReferenceValue = library;
        else Debug.LogWarning("[Patrimonio AR] No pude asignar la library por codigo: " +
                              "asignala a mano en Serialized Library.");

        // Este es, literalmente, el requisito del enunciado: seguimiento de multiples imagenes
        // en movimiento al mismo tiempo. Conviene mostrarlo en el video.
        var propMax = so.FindProperty("m_MaxNumberOfMovingImages");
        if (propMax != null) propMax.intValue = 3;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        if (origen.GetComponent<ImageTrackingSpawner>() == null)
        {
            origen.gameObject.AddComponent<ImageTrackingSpawner>();
            Debug.Log("[Patrimonio AR] Agregue el ImageTrackingSpawner al XR Origin.");
        }

        if (sesion.GetComponent<ARInputManager>() == null)
            sesion.gameObject.AddComponent<ARInputManager>();

        return true;
    }

    // ================================================================ Obelisco
    /// <summary>Devuelve la altura del diorama en metros reales.</summary>
    static float ConstruirObelisco(Transform padre, Lugar l)
    {
        var piedra   = Mat("Mat_Obelisco_Piedra", l.colorA, false);
        var granito  = Mat("Mat_Obelisco_Granito", l.colorB, false);
        var asfalto  = Mat("Mat_Asfalto", new Color(0.13f, 0.13f, 0.14f), false);
        var cesped   = Mat("Mat_Cesped", new Color(0.22f, 0.40f, 0.20f), false);
        var tronco   = Mat("Mat_Tronco", new Color(0.26f, 0.18f, 0.12f), false);
        var copa     = Mat("Mat_Copa", new Color(0.17f, 0.38f, 0.19f), false);
        var acento   = Mat("Mat_Obelisco_Acento", l.acento, true);

        // --- Plaza de la Republica
        Pieza(padre, "Plaza", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f),
              new Vector3(60f, 0.6f, 60f), granito);

        // canteros de cesped en las cuatro esquinas
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Pieza(padre, "Cantero", PrimitiveType.Cube,
                      new Vector3(sx * 19f, 0.75f, sz * 19f),
                      new Vector3(16f, 0.3f, 16f), cesped);

        // la 9 de Julio a los costados: da escala sin que haya que explicarla
        for (int sz = -1; sz <= 1; sz += 2)
        {
            Pieza(padre, "Avenida", PrimitiveType.Cube, new Vector3(0f, 0.25f, sz * 27f),
                  new Vector3(60f, 0.2f, 9f), asfalto);

            for (int i = 0; i < 4; i++)
            {
                var autoColor = Mat("Mat_Auto_" + i,
                    Color.HSVToRGB((i * 0.23f) % 1f, 0.65f, 0.85f), false);
                Pieza(padre, "Auto", PrimitiveType.Cube,
                      new Vector3(-22f + i * 15f, 1.0f, sz * 27f + (i % 2 == 0 ? 2.2f : -2.2f)),
                      new Vector3(4.4f, 1.4f, 1.9f), autoColor);
            }
        }

        // --- basamento
        Pieza(padre, "Basamento", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f),
              new Vector3(15f, 1.4f, 15f), granito);
        Pieza(padre, "Zocalo", PrimitiveType.Cube, new Vector3(0f, 2.0f, 0f),
              new Vector3(9f, 1.2f, 9f), piedra);

        // --- fuste troncopiramidal: 6,8 m de base, 3,0 m arriba, hasta los 61 m
        const float yArranque = 2.6f;
        const float yPiramidion = 61f;
        const float alturaTotal = 67.5f;

        var fuste = MallaPieza(padre, "Fuste",
            GuardarMalla(MallaTronco(6.8f, 3.0f, yPiramidion - yArranque), "Obelisco_Fuste"),
            new Vector3(0f, yArranque, 0f), piedra);
        fuste.transform.localRotation = Quaternion.identity;

        // --- piramidion
        MallaPieza(padre, "Piramidion",
            GuardarMalla(MallaTronco(3.0f, 0.02f, alturaTotal - yPiramidion), "Obelisco_Piramidion"),
            new Vector3(0f, yPiramidion, 0f), piedra);

        // banda de acento al pie del fuste
        Pieza(padre, "Banda", PrimitiveType.Cube, new Vector3(0f, 3.4f, 0f),
              new Vector3(7.1f, 0.5f, 7.1f), acento);

        // --- farolas
        for (int i = 0; i < 8; i++)
        {
            float angulo = i * Mathf.PI * 0.25f;
            var pos = new Vector3(Mathf.Cos(angulo) * 13f, 2.6f, Mathf.Sin(angulo) * 13f);
            Pieza(padre, "Farola", PrimitiveType.Capsule, pos, new Vector3(0.3f, 2.2f, 0.3f), granito);
            Pieza(padre, "Luz", PrimitiveType.Sphere, pos + new Vector3(0f, 2.2f, 0f),
                  Vector3.one * 0.8f, acento);
        }

        // --- arboles
        for (int i = 0; i < 10; i++)
        {
            float angulo = i * Mathf.PI * 0.2f + 0.3f;
            Arbol(padre, "Arbol",
                  new Vector3(Mathf.Cos(angulo) * 23f, 0.9f, Mathf.Sin(angulo) * 23f),
                  7f + (i % 3), tronco, copa);
        }


        return alturaTotal;
    }

    // ================================================================ Cataratas
    static float ConstruirCataratas(Transform padre, Lugar l)
    {
        var basalto = Mat("Mat_Basalto", l.colorA, false);
        var selva   = Mat("Mat_Selva", l.colorB, false);
        var tronco  = Mat("Mat_Tronco", new Color(0.26f, 0.18f, 0.12f), false);
        var agua    = MatTrans("Mat_Agua_Rio", new Color(0.42f, 0.74f, 0.78f), 0.75f, true);
        var espuma  = MatTrans("Mat_Espuma", new Color(0.95f, 0.98f, 1.00f), 0.80f, true);
        var niebla  = MatTrans("Mat_Niebla", new Color(0.92f, 0.96f, 0.98f), 0.28f, true);
        var lago    = MatTrans("Mat_Lago", new Color(0.16f, 0.36f, 0.34f), 0.80f, true);
        var acento  = Mat("Mat_Cataratas_Acento", l.acento, true);

        // Seccion modelada: 60 m del frente, con la garganta en el medio. El frente real tiene
        // 2,7 km, pero a 1:400 serian casi 7 metros de diorama.
        const float anchoSeccion = 60f;
        const float alturaSalto = 80f;
        const float anchoGarganta = 26f;

        // --- meseta de basalto, partida en tres bloques: la garganta es el hueco que queda
        Pieza(padre, "Meseta_Fondo", PrimitiveType.Cube, new Vector3(0f, alturaSalto * 0.5f, 34f),
              new Vector3(anchoSeccion, alturaSalto, 26f), basalto);

        for (int s = -1; s <= 1; s += 2)
        {
            float ancho = (anchoSeccion - anchoGarganta) * 0.5f;
            float centro = s * (anchoGarganta + ancho) * 0.5f;

            Pieza(padre, "Paredon", PrimitiveType.Cube,
                  new Vector3(centro, alturaSalto * 0.5f, 10f),
                  new Vector3(ancho, alturaSalto, 22f), basalto);

            // escalones de roca, que es como se ve el basalto de verdad
            for (int i = 0; i < 4; i++)
                Pieza(padre, "Escalon", PrimitiveType.Cube,
                      new Vector3(centro + s * 3f, alturaSalto - 14f - i * 16f, 1.5f - i * 1.2f),
                      new Vector3(ancho - i * 3f, 7f, 5f), basalto);
        }

        // --- rio arriba
        Pieza(padre, "Rio", PrimitiveType.Cube, new Vector3(0f, alturaSalto + 0.3f, 30f),
              new Vector3(anchoSeccion - 2f, 1.0f, 34f), agua);

        // --- la caida: vetas de agua animadas por CascadaAnimada
        var cascada = new GameObject("Cascada");
        cascada.transform.SetParent(padre, false);
        cascada.transform.localPosition = new Vector3(0f, 0f, 0f);

        for (int i = 0; i < 7; i++)
        {
            float x = -10.5f + i * 3.5f;
            var veta = Pieza(cascada.transform, "Veta", PrimitiveType.Cube,
                  new Vector3(x, alturaSalto - 8f, 0f),
                  new Vector3(3.2f, 16f, 2.4f), espuma);
            veta.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 3 - 1) * 2f);
        }

        var animacion = cascada.AddComponent<CascadaAnimada>();
        var soCascada = new SerializedObject(animacion);
        soCascada.FindProperty("alturaCaida").floatValue = alturaSalto;
        soCascada.FindProperty("velocidad").floatValue = 17f;
        soCascada.ApplyModifiedProperties();

        // cortina continua detras de las vetas, para que no se vea el fondo entre una y otra
        Pieza(padre, "Cortina", PrimitiveType.Cube, new Vector3(0f, alturaSalto * 0.5f, 1.2f),
              new Vector3(anchoGarganta - 2f, alturaSalto, 1.2f), agua);

        // --- niebla al pie
        for (int i = 0; i < 4; i++)
            Pieza(padre, "Niebla", PrimitiveType.Sphere,
                  new Vector3(-9f + i * 6f, 6f + (i % 2) * 4f, -4f - (i % 3) * 3f),
                  Vector3.one * (12f + i * 2f), niebla);

        // --- lago de abajo
        Pieza(padre, "Lago", PrimitiveType.Cube, new Vector3(0f, 0.4f, -26f),
              new Vector3(anchoSeccion, 0.8f, 52f), lago);

        // --- selva sobre el borde
        for (int i = 0; i < 16; i++)
        {
            float x = -28f + i * 3.8f;
            if (Mathf.Abs(x) < anchoGarganta * 0.5f + 2f) continue;   // no sobre la garganta
            Arbol(padre, "Arbol", new Vector3(x, alturaSalto, 16f + (i % 3) * 5f),
                  9f + (i % 4) * 2f, tronco, selva);
        }

        // --- pasarela con gente: la escala de los 80 m
        var pasarela = new GameObject("Pasarela");
        pasarela.transform.SetParent(padre, false);

        Pieza(pasarela.transform, "Tablero", PrimitiveType.Cube,
              new Vector3(-17f, alturaSalto + 1.2f, 2f), new Vector3(19f, 0.5f, 2.6f), acento);

        for (int i = 0; i < 8; i++)
            Pieza(pasarela.transform, "Baranda", PrimitiveType.Cube,
                  new Vector3(-25f + i * 2.4f, alturaSalto + 2.0f, 3.2f),
                  new Vector3(0.25f, 1.3f, 0.25f), acento);


        return alturaSalto + 15f;   // meseta + arboles
    }

    // ================================================================ Perito Moreno
    static float ConstruirGlaciar(Transform padre, Lugar l)
    {
        var hielo    = Mat("Mat_Hielo", l.colorA, false);
        var sombra   = Mat("Mat_Hielo_Sombra", l.colorB, false);
        var lago     = MatTrans("Mat_Lago_Argentino", new Color(0.26f, 0.52f, 0.56f), 0.82f, true);
        var espuma   = MatTrans("Mat_Salpicadura", new Color(0.96f, 0.99f, 1.00f), 0.70f, true);
        var acento   = Mat("Mat_Glaciar_Acento", l.acento, false);

        // Seccion modelada: 60 m del frente, que en la realidad tiene 5 km.
        const float anchoSeccion = 60f;
        const float alturaFrente = 70f;

        // --- masa del glaciar hacia atras
        Pieza(padre, "Masa", PrimitiveType.Cube, new Vector3(0f, alturaFrente * 0.5f, 42f),
              new Vector3(anchoSeccion, alturaFrente, 44f), sombra);

        // --- frente de hielo: 15 bloques irregulares, ninguno igual al anterior
        float[] alturas = { 58f, 66f, 71f, 62f, 70f, 55f, 68f, 70f, 64f, 59f, 69f, 66f, 57f, 63f, 67f };

        for (int i = 0; i < alturas.Length; i++)
        {
            float x = -27f + i * 3.86f;
            float ancho = 3.4f + (i % 3) * 0.7f;

            var bloque = Pieza(padre, "Bloque_Frente", PrimitiveType.Cube,
                  new Vector3(x, alturas[i] * 0.5f, 18f),
                  new Vector3(ancho, alturas[i], 20f), hielo);
            bloque.transform.localRotation = Quaternion.Euler(0f, (i % 5 - 2) * 4f, (i % 3 - 1) * 2.5f);

            // pico de hielo arriba
            Pieza(padre, "Pico", PrimitiveType.Cube,
                  new Vector3(x + (i % 2 == 0 ? 0.8f : -0.8f), alturas[i] + 3f, 16f),
                  new Vector3(ancho * 0.55f, 7f, 7f), hielo);

            // grieta
            if (i % 2 == 0)
                Pieza(padre, "Grieta", PrimitiveType.Cube,
                      new Vector3(x + 1.9f, alturas[i] * 0.5f, 7.6f),
                      new Vector3(0.6f, alturas[i] * 0.85f, 1.2f), sombra);
        }

        // --- lago Argentino
        Pieza(padre, "Lago", PrimitiveType.Cube, new Vector3(0f, 0.5f, -22f),
              new Vector3(anchoSeccion, 1.0f, 50f), lago);

        // --- tempanos
        for (int i = 0; i < 9; i++)
            Pieza(padre, "Tempano", PrimitiveType.Cube,
                  new Vector3(-25f + i * 6f, 1.4f, -8f - (i % 4) * 9f),
                  new Vector3(2.6f + (i % 3) * 1.2f, 2.4f, 3f + (i % 2) * 2f), hielo);

        // --- el desprendimiento: un bloque que se suelta y cae al lago
        var bloqueCae = Pieza(padre, "Bloque_Desprendido", PrimitiveType.Cube,
              new Vector3(7f, alturaFrente - 8f, 7f), new Vector3(5f, 11f, 6f), hielo);

        var salpicadura = Pieza(padre, "Salpicadura", PrimitiveType.Sphere,
              new Vector3(7f, 1f, 7f), Vector3.zero, espuma);

        var desprendimiento = padre.gameObject.AddComponent<DesprendimientoHielo>();
        var soDesp = new SerializedObject(desprendimiento);
        soDesp.FindProperty("bloque").objectReferenceValue = bloqueCae.transform;
        soDesp.FindProperty("salpicadura").objectReferenceValue = salpicadura.transform;
        soDesp.FindProperty("nivelAgua").floatValue = 1f;
        soDesp.FindProperty("intervalo").floatValue = 5f;
        soDesp.FindProperty("tamanoSalpicadura").floatValue = 16f;
        soDesp.ApplyModifiedProperties();

        // --- la lancha turistica: 20 m de eslora contra 70 m de hielo
        var lancha = new GameObject("Lancha");
        lancha.transform.SetParent(padre, false);

        Pieza(lancha.transform, "Casco", PrimitiveType.Cube, new Vector3(-17f, 1.8f, -16f),
              new Vector3(20f, 3f, 6f), acento);
        Pieza(lancha.transform, "Cabina", PrimitiveType.Cube, new Vector3(-19f, 4.4f, -16f),
              new Vector3(8f, 2.6f, 5f), Mat("Mat_Cabina", new Color(0.92f, 0.93f, 0.95f), false));


        return alturaFrente + 10f;
    }

    // ================================================================ ficha de datos
    static void ConstruirFicha(Transform padre, Lugar l, float alturaDiorama, Vector2 postal,
                               string[] datos, Material matPanel, TMP_FontAsset fuente)
    {
        // La ficha se dimensiona con el ancho de la postal como unidad. Es mas grande que la
        // tarjeta a proposito: el texto tiene que leerse desde la distancia a la que se sostiene
        // el telefono, sin tener que acercarlo. Con tres renglones cada uno queda al doble
        // de alto que con cinco, y el ancho de la ficha pasa a ser el limite, no el alto.
        float anchoPanel = postal.x * 1.08f;
        float altoPanel  = postal.x * 0.62f;

        var ficha = new GameObject("Ficha");
        ficha.transform.SetParent(padre, false);
        ficha.transform.localPosition = new Vector3(0f, alturaDiorama + altoPanel * 0.75f, 0f);
        ficha.AddComponent<FichaBillboard>();

        var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "Panel";
        panel.transform.SetParent(ficha.transform, false);
        panel.transform.localPosition = new Vector3(0f, 0f, 0.0025f);
        panel.transform.localScale = new Vector3(anchoPanel, altoPanel, 1f);
        Limpiar(panel, matPanel);

        var linea = GameObject.CreatePrimitive(PrimitiveType.Quad);
        linea.name = "Linea";
        linea.transform.SetParent(ficha.transform, false);
        linea.transform.localPosition = new Vector3(0f, altoPanel * 0.22f, 0.0015f);
        linea.transform.localScale = new Vector3(anchoPanel * 0.88f, altoPanel * 0.022f, 1f);
        Limpiar(linea, Mat("Mat_Ficha_" + l.imagen, l.acento, true));

        CrearTexto(ficha.transform, "Titulo", l.titulo, fuente,
                   new Vector3(0f, altoPanel * 0.35f, 0f),
                   anchoPanel * 0.90f, altoPanel * 0.15f,
                   new Color(1f, 0.97f, 0.90f));

        // En negrita y en blanco puro: el trazo fino de la fuente normal, al tamano que ocupa
        // en pantalla, se come los pixeles y el texto se ve lavado.
        CrearTexto(ficha.transform, "Datos", string.Join("\n", datos), fuente,
                   new Vector3(0f, -altoPanel * 0.09f, 0f),
                   anchoPanel * 0.94f, altoPanel * 0.62f,
                   Color.white);
    }

    /// <summary>
    /// Crea un texto 3D y lo escala midiendo el mesh ya generado, asi queda del tamano pedido
    /// en metros sin depender de la unidad interna de TextMeshPro.
    /// </summary>
    static TextMeshPro CrearTexto(Transform padre, string nombre, string contenido,
                                  TMP_FontAsset fuente, Vector3 posicion,
                                  float anchoMetros, float altoMetros, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<RectTransform>();

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = fuente;
        tmp.text = contenido;
        tmp.color = color;
        tmp.fontSize = 8f;
        tmp.enableAutoSizing = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.lineSpacing = 6f;

        // La negrita se aplica ANTES de medir: cambia el ancho de los glifos, y si se midiera
        // en redonda el texto quedaria mas ancho que el panel.
        tmp.fontStyle = FontStyles.Bold;

        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100f, 60f);

        tmp.ForceMeshUpdate();
        Bounds b = tmp.textBounds;

        float escala = 0.01f;
        if (b.size.x > 0.01f && b.size.y > 0.01f)
            escala = Mathf.Min(anchoMetros / b.size.x, altoMetros / b.size.y);

        go.transform.localScale = Vector3.one * escala;
        go.transform.localPosition = posicion;
        return tmp;
    }

    // ================================================================ cableado
    static void CablearSpawner(List<string> nombres, List<GameObject> prefabs)
    {
        var spawner = Object.FindFirstObjectByType<ImageTrackingSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[Patrimonio AR] No encontre un ImageTrackingSpawner en la escena.");
            return;
        }

        var so = new SerializedObject(spawner);
        var lista = so.FindProperty("pares");
        if (lista == null)
        {
            Debug.LogWarning("[Patrimonio AR] El ImageTrackingSpawner no expone la lista pares.");
            return;
        }

        lista.arraySize = nombres.Count;
        for (int i = 0; i < nombres.Count; i++)
        {
            var elemento = lista.GetArrayElementAtIndex(i);
            elemento.FindPropertyRelative("nombreImagen").stringValue = nombres[i];
            elemento.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);
        Debug.Log("[Patrimonio AR] ImageTrackingSpawner cableado con " + nombres.Count + " lugares.");
    }

    static void CrearHud(TMP_FontAsset fuente)
    {
        var anterior = GameObject.Find("HUD Tracking");
        if (anterior != null) Object.DestroyImmediate(anterior);

        var hud = new GameObject("HUD Tracking");
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var escalador = hud.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1080f, 1920f);
        escalador.matchWidthOrHeight = 0.5f;

        hud.AddComponent<GraphicRaycaster>();

        // El panel va chico y pegado a una esquina, no cruzando la pantalla. En Realidad
        // Aumentada toda interfaz tapa el mundo real, asi que ocupa lo minimo indispensable
        // para dejar demostrado el seguimiento simultaneo y nada mas.
        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(hud.transform, false);
        var imagen = fondo.AddComponent<Image>();
        imagen.color = new Color(0f, 0f, 0f, 0.55f);
        AnclarEsquina(fondo.GetComponent<RectTransform>(), 404f, 128f, 24f);

        var textoGo = new GameObject("Texto");
        textoGo.transform.SetParent(fondo.transform, false);
        var texto = textoGo.AddComponent<TextMeshProUGUI>();
        texto.font = fuente;
        texto.fontSize = 25f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.text = "Apunta a una postal";

        var rt = textoGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, 12f);
        rt.offsetMax = new Vector2(-16f, -12f);

        var panel = hud.AddComponent<PanelEstadoTracking>();
        var so = new SerializedObject(panel);
        so.FindProperty("manager").objectReferenceValue =
            Object.FindFirstObjectByType<ARTrackedImageManager>();
        so.FindProperty("texto").objectReferenceValue = texto;
        so.ApplyModifiedProperties();

        Debug.Log("[Patrimonio AR] HUD de estado de tracking creado.");
    }

    /// <summary>
    /// Ancla el panel ABAJO A LA DERECHA, con tamano fijo. Es la esquina que menos molesta:
    /// la vista se dirige al centro, donde esta el diorama, y la mano que sostiene el telefono
    /// ya esta de ese lado.
    /// </summary>
    static void AnclarEsquina(RectTransform rt, float ancho, float alto, float margen)
    {
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(ancho, alto);
        rt.anchoredPosition = new Vector2(-margen, margen);
    }

    // ================================================================ helpers de modelado
    static GameObject Pieza(Transform padre, string nombre, PrimitiveType tipo,
                            Vector3 posicion, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        Limpiar(go, mat);
        return go;
    }

    static GameObject MallaPieza(Transform padre, string nombre, Mesh malla,
                                 Vector3 posicion, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;

        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var render = go.AddComponent<MeshRenderer>();
        render.sharedMaterial = mat;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        return go;
    }


    static GameObject Arbol(Transform padre, string nombre, Vector3 pies, float altura,
                            Material tronco, Material copa)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pies;

        Pieza(go.transform, "Tronco", PrimitiveType.Cylinder,
              new Vector3(0f, altura * 0.28f, 0f),
              new Vector3(altura * 0.09f, altura * 0.28f, altura * 0.09f), tronco);

        Pieza(go.transform, "Copa", PrimitiveType.Sphere,
              new Vector3(0f, altura * 0.72f, 0f),
              new Vector3(altura * 0.58f, altura * 0.52f, altura * 0.58f), copa);

        return go;
    }

    static void Limpiar(GameObject go, Material mat)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        var render = go.GetComponent<MeshRenderer>();
        if (render != null)
        {
            render.sharedMaterial = mat;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    // ================================================================ malla troncopiramidal
    /// <summary>
    /// Tronco de piramide de base cuadrada, de y=0 a y=altura. Unity no trae una primitiva
    /// con forma de obelisco, asi que la malla se genera: cuatro caras laterales mas las dos
    /// tapas, con vertices propios por cara para que el sombreado quede plano y marcado.
    /// Con ladoTope cerca de cero, la misma funcion devuelve el piramidion.
    /// </summary>
    static Mesh MallaTronco(float ladoBase, float ladoTope, float altura)
    {
        float b = ladoBase * 0.5f;
        float t = ladoTope * 0.5f;

        var abajo = new[]
        {
            new Vector3(-b, 0f, -b), new Vector3(b, 0f, -b),
            new Vector3(b, 0f, b),   new Vector3(-b, 0f, b)
        };

        var arriba = new[]
        {
            new Vector3(-t, altura, -t), new Vector3(t, altura, -t),
            new Vector3(t, altura, t),   new Vector3(-t, altura, t)
        };

        var vertices = new List<Vector3>();
        var triangulos = new List<int>();

        void Cara(Vector3 a, Vector3 bb, Vector3 c, Vector3 d)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(bb); vertices.Add(c); vertices.Add(d);
            triangulos.Add(i); triangulos.Add(i + 2); triangulos.Add(i + 1);
            triangulos.Add(i); triangulos.Add(i + 3); triangulos.Add(i + 2);
        }

        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            Cara(abajo[i], abajo[j], arriba[j], arriba[i]);
        }

        Cara(arriba[0], arriba[1], arriba[2], arriba[3]);                 // tapa
        Cara(abajo[3], abajo[2], abajo[1], abajo[0]);                     // base

        // En lugar de confiar en haber acertado el orden de los vertices a mano, se verifica:
        // si la normal de un triangulo apunta hacia el centro de la malla, se da vuelta. Asi
        // ninguna cara puede quedar invertida (que es como desaparece un objeto en Unity).
        OrientarHaciaAfuera(vertices, triangulos, new Vector3(0f, altura * 0.5f, 0f));

        var malla = new Mesh { name = "Tronco" };
        malla.SetVertices(vertices);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    /// <summary>
    /// Para cada triangulo compara su normal con la direccion que va del centro de la malla
    /// al centro del triangulo. Si apuntan al reves, invierte el orden de dos vertices.
    /// </summary>
    static void OrientarHaciaAfuera(List<Vector3> vertices, List<int> triangulos, Vector3 centro)
    {
        for (int t = 0; t + 2 < triangulos.Count; t += 3)
        {
            Vector3 a = vertices[triangulos[t]];
            Vector3 b = vertices[triangulos[t + 1]];
            Vector3 c = vertices[triangulos[t + 2]];

            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 haciaAfuera = (a + b + c) / 3f - centro;

            if (Vector3.Dot(normal, haciaAfuera) < 0f)
            {
                int aux = triangulos[t + 1];
                triangulos[t + 1] = triangulos[t + 2];
                triangulos[t + 2] = aux;
            }
        }
    }

    static Mesh GuardarMalla(Mesh malla, string nombre)
    {
        string ruta = kCarpetaMallas + "/" + nombre + ".asset";
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);

        if (existente != null)
        {
            // Se reescribe la malla que ya esta, asi los prefabs no pierden la referencia.
            existente.Clear();
            existente.SetVertices(new List<Vector3>(malla.vertices));
            existente.SetTriangles(malla.triangles, 0);
            existente.RecalculateNormals();
            existente.RecalculateBounds();
            EditorUtility.SetDirty(existente);
            Object.DestroyImmediate(malla);
            return existente;
        }

        malla.name = nombre;
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }

    // ================================================================ materiales
    static Material Mat(string nombre, Color color, bool unlit)
    {
        string ruta = kCarpetaMateriales + "/" + nombre + ".mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        Shader shader = unlit
            ? Shader.Find("Universal Render Pipeline/Unlit")
            : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");

        var mat = existente != null ? existente : new Material(shader);
        mat.shader = shader;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.14f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.14f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        if (existente == null) AssetDatabase.CreateAsset(mat, ruta);
        else EditorUtility.SetDirty(mat);

        return mat;
    }

    /// <summary>
    /// Material transparente de URP. Poner el Surface Type en Transparent por codigo no es
    /// un solo campo: hay que tocar los modos de blend, el ZWrite, el keyword del shader y
    /// la cola de render. Si falta alguno, el objeto sale opaco.
    /// </summary>
    static Material MatTrans(string nombre, Color color, float alpha, bool unlit)
    {
        var mat = Mat(nombre, new Color(color.r, color.g, color.b, alpha), unlit);

        mat.SetFloat("_Surface", 1f);                 // 0 opaco, 1 transparente
        mat.SetFloat("_Blend", 0f);                   // alpha blend
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", new Color(color.r, color.g, color.b, alpha));

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================================================================ varios
    static void AsegurarCarpetas()
    {
        if (!AssetDatabase.IsValidFolder(kCarpetaRaiz))
            AssetDatabase.CreateFolder("Assets", "Patrimonio");
        if (!AssetDatabase.IsValidFolder(kCarpetaMateriales))
            AssetDatabase.CreateFolder(kCarpetaRaiz, "Materiales");
        if (!AssetDatabase.IsValidFolder(kCarpetaPrefabs))
            AssetDatabase.CreateFolder(kCarpetaRaiz, "Prefabs");
        if (!AssetDatabase.IsValidFolder(kCarpetaMallas))
            AssetDatabase.CreateFolder(kCarpetaRaiz, "Mallas");
    }

    static TMP_FontAsset BuscarFuente()
    {
        TMP_FontAsset fuente = null;

        try { fuente = TMP_Settings.defaultFontAsset; }
        catch { fuente = null; }

        if (fuente == null)
            fuente = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (fuente == null)
        {
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        return fuente;
    }

    public static Vector2 TamanoDeLaPostal(XRReferenceImageLibrary library, string nombre)
    {
        for (int i = 0; i < library.count; i++)
        {
            var imagen = library[i];
            if (imagen.name != nombre) continue;
            if (imagen.size.x <= 0f)
                return new Vector2(GeneradorPostales.kAnchoPostal, GeneradorPostales.kAltoPostal);
            return imagen.size;
        }
        return Vector2.zero;
    }
}
