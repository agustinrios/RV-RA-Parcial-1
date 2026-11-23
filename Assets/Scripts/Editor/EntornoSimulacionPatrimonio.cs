using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Prepara el entorno de XR Simulation: una mesa con las tres postales acostadas encima,
/// y la camara arrancando por encima de ellas, como quien sostiene el telefono sobre la mesa.
///
/// Dos decisiones de fondo:
///
/// 1. Los marcadores simulados se CLONAN de uno que ya trae el entorno de ejemplo, porque un
///    SimulatedTrackedImage necesita MeshFilter y MeshRenderer propios y Unity le genera la
///    malla en OnEnable. Clonando uno que ya funciona, ese cableado viene hecho.
///
/// 2. La mesa la crea este script, no se usa un mueble del entorno. Asi la posicion y la altura
///    de las postales no dependen de donde haya puesto Unity los cuadros o las vitrinas de la
///    sala de ejemplo, que es lo que hacia que aparecieran colgadas en una pared.
///
/// Orientacion, que es el punto que importa: SimulatedTrackedImage.CreateQuadMesh() arma la
/// malla con los cuatro vertices en y = 0, o sea en el plano XZ local, mirando hacia el +Y
/// local. Por lo tanto una postal ACOSTADA mirando hacia arriba es, simplemente, rotacion
/// identidad. Una postal con rotacion X = -90 queda parada.
///
/// Menu: Patrimonio AR / 3
/// </summary>
public static class EntornoSimulacionPatrimonio
{
    const string kRutaMuseo =
        "Assets/UnityXRContent/ARFoundation/SimulationEnvironments/Museum/Museum_69ftx48ft.prefab";
    const string kCarpetaEntornos = "Assets/Entornos";
    const string kRutaEntorno = "Assets/Entornos/EntornoPatrimonio.prefab";
    const string kRutaMatMesa = "Assets/Entornos/Mat_Mesa.mat";
    const string kRutaLibrary = "Assets/Recursos/PatrimonioLibrary.asset";
    const string kRutaPreferencias = "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";
    const string kRutaManager = "Assets/XR/UserSimulationSettings/SimulationEnvironmentAssetsManager.asset";

    const string kNombreMesa = "Mesa Patrimonio";

    // Altura de una mesa comun. Es la que da la vista natural de alguien parado al lado.
    const float kAlturaMesa = 0.75f;
    const float kAnchoMesa = 1.10f;
    const float kFondoMesa = 0.70f;

    // Separacion entre postal y postal. Con postales de 15 cm deja 11 cm de aire entre una y
    // otra: entran las tres en cuadro y los dioramas no se pisan.
    const float kSeparacion = 0.26f;

    // Las postales se apoyan 2 mm por encima de la tapa. Dos superficies exactamente a la misma
    // altura producen z-fighting: la placa de video no puede decidir cual va adelante y las dos
    // parpadean.
    const float kAlturaSobreMesa = 0.002f;

    [MenuItem("Patrimonio AR/3 - Preparar entorno de XR Simulation", false, 40)]
    public static void Preparar()
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(kRutaLibrary);
        if (library == null || library.count == 0)
        {
            EditorUtility.DisplayDialog("Falta la library",
                "No encontre imagenes en " + kRutaLibrary + "\n\n" +
                "Corre primero: Patrimonio AR > 1 - Generar postales y library.", "Cerrar");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(kRutaMuseo) == null)
        {
            EditorUtility.DisplayDialog("Falta el entorno de ejemplo",
                "No encontre " + kRutaMuseo + "\n\n" +
                "Son los entornos de ejemplo de XR Simulation. Instalalos desde\n" +
                "Window > XR > AR Foundation > XR Environment, boton Install.", "Cerrar");
            return;
        }

        if (!AssetDatabase.IsValidFolder(kCarpetaEntornos))
            AssetDatabase.CreateFolder("Assets", "Entornos");

        // Copia propia del entorno: no se toca el prefab original del paquete de ejemplos.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(kRutaEntorno) == null)
        {
            if (!AssetDatabase.CopyAsset(kRutaMuseo, kRutaEntorno))
            {
                EditorUtility.DisplayDialog("Error",
                    "No pude copiar el entorno a " + kRutaEntorno, "Cerrar");
                return;
            }
            AssetDatabase.Refresh();
            Debug.Log("[Patrimonio AR] Entorno copiado a " + kRutaEntorno);
        }

        var matMesa = MaterialMesa();

        var raiz = PrefabUtility.LoadPrefabContents(kRutaEntorno);
        if (raiz == null)
        {
            EditorUtility.DisplayDialog("Error", "No pude abrir " + kRutaEntorno, "Cerrar");
            return;
        }

        int creadas = 0;

        try
        {
            Vector3 tapaMesa = CrearMesa(raiz, matMesa);
            creadas = ArmarPostales(raiz, library, tapaMesa, out Vector3 centroPostales);

            if (creadas == 0)
            {
                EditorUtility.DisplayDialog("Sin marcadores de referencia",
                    "El entorno no tiene ningun SimulatedTrackedImage para usar de molde.\n\n" +
                    "Elegi otro entorno (Museum, Factory o Billboard traen cuadros) o agrega uno " +
                    "a mano con Add Component > Simulated Tracked Image.", "Cerrar");
                return;
            }

            ApuntarCamaraALasPostales(raiz, centroPostales);

            PrefabUtility.SaveAsPrefabAsset(raiz, kRutaEntorno);
            Debug.Log("[Patrimonio AR] " + creadas + " postales acostadas sobre la mesa en " + kRutaEntorno);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        SeleccionarEntorno();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Patrimonio AR",
            "Entorno listo: " + kRutaEntorno + "\n\n" +
            "Las tres postales quedaron ACOSTADAS sobre una mesa de " + (kAlturaMesa * 100f) +
            " cm, separadas " + (kSeparacion * 100f) + " cm, y la camara arranca por encima " +
            "mirandolas en diagonal.\n\n" +
            "Dale Play. WASD para moverte, click derecho + mouse para mirar.\n\n" +
            "Si despues movés las postales a mano, hacelo abriendo el prefab " +
            "(doble click en EntornoPatrimonio) y NO vuelvas a correr este menu, porque " +
            "rehace los objetos Postal_*.", "Listo");
    }

    // ================================================================ mesa
    /// <summary>
    /// Crea la mesa sobre la que se apoyan las postales y devuelve el centro de la tapa.
    /// Se ubica en el centro de la sala, que se deduce del area por la que la camara simulada
    /// puede moverse.
    /// </summary>
    static Vector3 CrearMesa(GameObject raiz, Material material)
    {
        var anterior = BuscarHijo(raiz.transform, kNombreMesa);
        if (anterior != null) Object.DestroyImmediate(anterior.gameObject);

        Vector3 centroSala = CentroDeLaSala(raiz);

        var mesa = new GameObject(kNombreMesa);
        mesa.transform.SetParent(raiz.transform, false);
        mesa.transform.position = new Vector3(centroSala.x, 0f, centroSala.z);

        // tapa
        Pieza(mesa.transform, "Tapa", new Vector3(0f, kAlturaMesa - 0.02f, 0f),
              new Vector3(kAnchoMesa, 0.04f, kFondoMesa), material);

        // patas
        float px = kAnchoMesa * 0.5f - 0.07f;
        float pz = kFondoMesa * 0.5f - 0.07f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Pieza(mesa.transform, "Pata",
                      new Vector3(sx * px, (kAlturaMesa - 0.04f) * 0.5f, sz * pz),
                      new Vector3(0.05f, kAlturaMesa - 0.04f, 0.05f), material);

        Vector3 tapa = new Vector3(centroSala.x, kAlturaMesa, centroSala.z);
        Debug.Log("[Patrimonio AR] Mesa creada con la tapa en " + tapa);
        return tapa;
    }

    static Vector3 CentroDeLaSala(GameObject raiz)
    {
        foreach (var comp in raiz.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;
            if (comp.GetType().Name != "SimulationEnvironment") continue;

            var so = new SerializedObject(comp);
            var prop = so.FindProperty("m_CameraMovementBounds");

            if (prop != null && prop.propertyType == SerializedPropertyType.Bounds)
            {
                var b = prop.boundsValue;
                return new Vector3(b.center.x, 0f, b.center.z);
            }
        }

        return Vector3.zero;
    }

    static GameObject Pieza(Transform padre, string nombre, Vector3 posicion, Vector3 escala,
                            Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;

        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        var render = go.GetComponent<MeshRenderer>();
        if (render != null) render.sharedMaterial = material;

        return go;
    }

    static Material MaterialMesa()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Material>(kRutaMatMesa);
        if (existente != null) return existente;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = "Mat_Mesa" };

        var color = new Color(0.46f, 0.37f, 0.28f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        AssetDatabase.CreateAsset(mat, kRutaMatMesa);
        return mat;
    }

    // ================================================================ postales
    /// <summary>
    /// Desactiva los marcadores originales del entorno y deja en su lugar tres clones con las
    /// postales del proyecto, ACOSTADOS sobre la tapa de la mesa.
    ///
    /// Los originales HAY que desactivarlos: XR Simulation rastrea todos los SimulatedTrackedImage
    /// del entorno, esten o no en la Reference Image Library, y para los que no estan AR Foundation
    /// no puede resolver la imagen de referencia (el nombre vuelve nulo). El spawner ya los ignora,
    /// pero si quedan activos ensucian el HUD y el conteo.
    /// </summary>
    static int ArmarPostales(GameObject raiz, XRReferenceImageLibrary library, Vector3 tapaMesa,
                             out Vector3 centro)
    {
        centro = tapaMesa;

        var existentes = new List<MonoBehaviour>();

        foreach (var comp in raiz.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;
            if (comp.GetType().Name != "SimulatedTrackedImage") continue;

            // Un clon de una corrida anterior de este menu: se borra y se vuelve a crear.
            if (comp.gameObject.name.StartsWith("Postal_"))
            {
                Object.DestroyImmediate(comp.gameObject);
                continue;
            }

            existentes.Add(comp);
        }

        if (existentes.Count == 0) return 0;

        var molde = existentes[0];
        var padre = raiz.transform;

        foreach (var comp in existentes)
            comp.gameObject.SetActive(false);

        int creadas = 0;
        float alturaPostales = tapaMesa.y + kAlturaSobreMesa;

        for (int i = 0; i < library.count; i++)
        {
            var referencia = library[i];
            var textura = referencia.texture;

            // Segun como se haya guardado la library, la referencia a la textura puede volver
            // nula, y tambien vuelve nula si el archivo que apuntaba se borro. En ese caso se
            // busca por nombre, probando los nombres con los que el menu 1 deja los archivos.
            if (textura == null)
                foreach (var candidato in new[]
                         {
                             "Assets/Img/Carta_" + referencia.name + ".png",
                             "Assets/Img/" + referencia.name + ".png",
                             "Assets/Img/" + referencia.name + ".jpg",
                             "Assets/Img/" + referencia.name + ".jpeg"
                         })
                {
                    textura = AssetDatabase.LoadAssetAtPath<Texture2D>(candidato);
                    if (textura != null) break;
                }

            if (textura == null)
            {
                Debug.LogWarning("[Patrimonio AR] La imagen '" + referencia.name +
                                 "' de la library no tiene textura. Corre primero el menu 1, " +
                                 "que reconstruye la library con los archivos que hay hoy en " +
                                 "Assets/Img. La salteo.");
                continue;
            }

            var clon = Object.Instantiate(molde.gameObject, padre);
            clon.name = "Postal_" + referencia.name;
            clon.SetActive(true);

            // Las tres en fila sobre el eje X, acostadas sobre la mesa.
            // Rotacion identidad = imagen hacia arriba, porque la malla del marcador simulado
            // se construye en el plano XZ local mirando al +Y local.
            float desplazamiento = (i - (library.count - 1) * 0.5f) * kSeparacion;

            clon.transform.SetPositionAndRotation(
                new Vector3(tapaMesa.x + desplazamiento, alturaPostales, tapaMesa.z),
                Quaternion.identity);
            clon.transform.localScale = Vector3.one;

            foreach (var comp in clon.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "SimulatedTrackedImage") continue;

                var so = new SerializedObject(comp);

                var propImagen = so.FindProperty("m_Image");
                if (propImagen != null) propImagen.objectReferenceValue = textura;

                // El tamano fisico tiene que ser EL MISMO que declara la library: es lo que usa
                // el sistema para calcular a que distancia esta la postal.
                var propTamano = so.FindProperty("m_ImagePhysicalSizeMeters");
                if (propTamano != null)
                    propTamano.vector2Value = referencia.size.x > 0f
                        ? referencia.size
                        : new Vector2(GeneradorPostales.kAnchoPostal, GeneradorPostales.kAltoPostal);

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
            }

            creadas++;
            Debug.Log("[Patrimonio AR] Postal acostada: " + clon.name + " en " +
                      clon.transform.position + " (" + referencia.size.x + " x " +
                      referencia.size.y + " m)");
        }

        centro = new Vector3(tapaMesa.x, alturaPostales, tapaMesa.z);
        return creadas;
    }

    // ================================================================ camara
    /// <summary>
    /// Deja la camara simulada arrancando por encima de las postales y en diagonal, que es como
    /// se sostiene el telefono sobre una mesa.
    ///
    /// No se la pone perfectamente cenital a proposito: los dioramas son objetos que se paran
    /// sobre la postal, y vistos desde arriba a pico quedan completamente escorzados. Con la
    /// camara a unos 50 grados se ve la altura de cada monumento, que es todo el concepto.
    /// </summary>
    static void ApuntarCamaraALasPostales(GameObject raiz, Vector3 centroPostales)
    {
        MonoBehaviour entorno = null;

        foreach (var comp in raiz.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;
            if (comp.GetType().Name != "SimulationEnvironment") continue;
            entorno = comp;
            break;
        }

        if (entorno == null)
        {
            Debug.LogWarning("[Patrimonio AR] El entorno no tiene componente SimulationEnvironment: " +
                             "no pude mover la camara de arranque.");
            return;
        }

        var so = new SerializedObject(entorno);

        // 45 cm por encima y 38 cm por delante: unos 50 grados sobre la mesa.
        Vector3 posicion = centroPostales + new Vector3(0f, 0.45f, -0.38f);
        Vector3 objetivo = centroPostales + new Vector3(0f, 0.06f, 0f);

        Quaternion rotacion = Quaternion.LookRotation((objetivo - posicion).normalized, Vector3.up);

        EscribirPose(so, "m_CameraStartingPose", posicion, rotacion);
        EscribirPose(so, "m_DefaultViewPose", posicion, rotacion);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(entorno);

        Debug.Log("[Patrimonio AR] Camara simulada en " + posicion + " mirando las postales de " +
                  centroPostales);
    }

    static void EscribirPose(SerializedObject so, string nombre, Vector3 posicion, Quaternion rotacion)
    {
        var pose = so.FindProperty(nombre);
        if (pose == null) return;

        var propPos = pose.FindPropertyRelative("position");
        var propRot = pose.FindPropertyRelative("rotation");

        if (propPos != null) propPos.vector3Value = posicion;
        if (propRot != null) propRot.quaternionValue = rotacion;
    }

    // ================================================================ seleccion del entorno
    /// <summary>
    /// Deja el entorno elegido en las preferencias de XR Simulation y lo agrega a la lista
    /// que muestra la ventana XR Environment, para no tener que buscarlo en el desplegable.
    /// </summary>
    static void SeleccionarEntorno()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kRutaEntorno);
        if (prefab == null) return;

        var preferencias = AssetDatabase.LoadAssetAtPath<Object>(kRutaPreferencias);
        if (preferencias != null)
        {
            var so = new SerializedObject(preferencias);
            var prop = so.FindProperty("m_EnvironmentPrefab");

            if (prop != null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(preferencias);
                Debug.Log("[Patrimonio AR] Entorno seleccionado en XRSimulationPreferences.");
            }
        }
        else
        {
            Debug.LogWarning("[Patrimonio AR] No encontre " + kRutaPreferencias +
                             ". Elegi el entorno a mano en Window > XR > AR Foundation > XR Environment.");
        }

        var manager = AssetDatabase.LoadAssetAtPath<Object>(kRutaManager);
        if (manager == null) return;

        var soManager = new SerializedObject(manager);
        var lista = soManager.FindProperty("m_EnvironmentPrefabPaths");
        if (lista == null || !lista.isArray) return;

        for (int i = 0; i < lista.arraySize; i++)
            if (lista.GetArrayElementAtIndex(i).stringValue == kRutaEntorno) return;

        lista.InsertArrayElementAtIndex(lista.arraySize);
        lista.GetArrayElementAtIndex(lista.arraySize - 1).stringValue = kRutaEntorno;
        soManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        Debug.Log("[Patrimonio AR] Entorno agregado a la lista de la ventana XR Environment.");
    }

    static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre)
            if (hijo.name == nombre) return hijo;
        return null;
    }
}
