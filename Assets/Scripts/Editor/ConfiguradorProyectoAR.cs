using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

/// <summary>
/// Configuracion del proyecto para la entrega y, sobre todo, VERIFICACION.
///
/// La mayoria de los problemas de un proyecto de AR no dan error: simplemente no pasa nada
/// al dar Play. Este menu revisa de una vez las seis cosas que lo provocan y dice cual falta,
/// en lugar de tener que ir a buscarlas una por una a seis ventanas distintas.
///
/// Menu: Patrimonio AR / 0 y 4
/// </summary>
public static class ConfiguradorProyectoAR
{
    // ------------------------------------------------------------------ datos de la entrega
    const string kNombreAlumno = "Rios Daniel Agustin";
    const string kCompany      = "Rios Daniel Agustin";
    const string kProducto     = "Patrimonio AR";
    const string kPaquete      = "com.rios.patrimonioar";

    const string kRutaEscena  = "Assets/Scenes/SampleScene.unity";
    const string kRutaLibrary = "Assets/Recursos/PatrimonioLibrary.asset";
    const string kRutaXR      = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

    const string kLoaderARCore     = "UnityEngine.XR.ARCore.ARCoreLoader";
    const string kLoaderSimulacion = "UnityEngine.XR.Simulation.SimulationLoader";

    // ================================================================ MENU 0
    [MenuItem("Patrimonio AR/0 - Configurar proyecto y verificar", false, 10)]
    public static void Configurar()
    {
        // Los recursos esenciales de TextMeshPro no vienen importados en un proyecto nuevo,
        // y son los que traen la fuente de la ficha de datos y del HUD.
        if (BuscarFuente() == null)
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Patrimonio AR",
                "Faltaban los recursos esenciales de TextMeshPro (la fuente de la ficha y del " +
                "HUD). Los acabo de importar.\n\n" +
                "Cuando Unity termine de importar y recompilar, volve a correr este menu.", "Listo");
            return;
        }

        AjustarPlayerSettings();
        RegistrarXR();

        string reporte = Verificar();
        Debug.Log("[Patrimonio AR] Verificacion:\n" + reporte);

        EditorUtility.DisplayDialog("Patrimonio AR - verificacion", reporte, "Listo");
    }

    static void AjustarPlayerSettings()
    {
        PlayerSettings.companyName = kCompany;
        PlayerSettings.productName = kProducto;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, kPaquete);
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        // IL2CPP y ARM64: ARCore no funciona con Mono, y ARMv7 ya no se puede publicar.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // ARCore pide API 24 como minimo; 25 es el valor que usa la plantilla de AR.
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

        // Solo OpenGLES3. Si Vulkan queda primero, en varios dispositivos la camara de AR
        // sale en negro: se ve la interfaz pero no la imagen de la camara.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { GraphicsDeviceType.OpenGLES3 });

        AssetDatabase.SaveAssets();
        Debug.Log("[Patrimonio AR] Player Settings de Android ajustados.");
    }

    /// <summary>
    /// Deja el asset de XR Plug-in Management registrado en el proyecto y, si puede, asigna
    /// los dos loaders: ARCore para Android y XR Simulation para Standalone.
    ///
    /// El detalle que mas se pasa por alto: XR Simulation se configura en la pestana
    /// Standalone, NO en Android. Sin eso, al dar Play no pasa absolutamente nada y parece
    /// que el proyecto esta roto.
    /// </summary>
    static void RegistrarXR()
    {
        try
        {
            var porPlataforma = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(kRutaXR);

            if (porPlataforma == null)
            {
                Debug.LogWarning("[Patrimonio AR] No encontre " + kRutaXR +
                                 ". Abri una vez Edit > Project Settings > XR Plug-in Management " +
                                 "(eso crea el asset) y volve a correr este menu.");
                return;
            }

            // Que el asset exista no alcanza: tiene que estar registrado como config object
            // del proyecto, que es de donde lo lee XR Management al arrancar.
            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(
                    XRGeneralSettings.k_SettingsKey, out _))
            {
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porPlataforma, true);
                Debug.Log("[Patrimonio AR] Registre el asset de XR Plug-in Management en el proyecto.");
            }

            AsignarLoader(porPlataforma, BuildTargetGroup.Android, kLoaderARCore);
            AsignarLoader(porPlataforma, BuildTargetGroup.Standalone, kLoaderSimulacion);

            EditorUtility.SetDirty(porPlataforma);
            AssetDatabase.SaveAssets();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Patrimonio AR] No pude configurar XR por codigo (" + e.Message +
                             "). Hacelo a mano en Edit > Project Settings > XR Plug-in Management: " +
                             "Android > Google ARCore, y PC Mac & Linux Standalone > XR Simulation.");
        }
    }

    static void AsignarLoader(XRGeneralSettingsPerBuildTarget porPlataforma,
                              BuildTargetGroup grupo, string tipoLoader)
    {
        var general = porPlataforma.SettingsForBuildTarget(grupo);

        if (general == null)
        {
            general = ScriptableObject.CreateInstance<XRGeneralSettings>();
            general.Manager = ScriptableObject.CreateInstance<XRManagerSettings>();

            AssetDatabase.AddObjectToAsset(general, porPlataforma);
            AssetDatabase.AddObjectToAsset(general.Manager, porPlataforma);

            porPlataforma.SetSettingsForBuildTarget(grupo, general);
        }

        if (general.Manager == null)
        {
            general.Manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            AssetDatabase.AddObjectToAsset(general.Manager, porPlataforma);
        }

        if (XRPackageMetadataStore.AssignLoader(general.Manager, tipoLoader, grupo))
            Debug.Log("[Patrimonio AR] Loader " + tipoLoader + " asignado en " + grupo + ".");
        else
            Debug.LogWarning("[Patrimonio AR] No pude asignar " + tipoLoader + " en " + grupo +
                             " (puede que ya estuviera asignado).");
    }

    // ================================================================ verificacion
    static string Verificar()
    {
        var sb = new StringBuilder();

        // --- 1. loaders de XR
        string loadersAndroid = LoadersDe(BuildTargetGroup.Android);
        string loadersPC = LoadersDe(BuildTargetGroup.Standalone);

        Linea(sb, loadersAndroid.Contains("ARCore"),
              "XR Android: Google ARCore",
              "XR Android: falta ARCore -> Project Settings > XR Plug-in Management > Android");

        Linea(sb, loadersPC.Contains("Simulation"),
              "XR Standalone: XR Simulation",
              "XR Standalone: falta XR Simulation -> Project Settings > XR Plug-in Management > " +
              "PC, Mac & Linux Standalone (OJO: va aca, no en Android)");

        // --- 2. AR Background Renderer Feature en los renderers de URP
        int sinFeature = 0, totalRenderers = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
        {
            var datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (datos == null) continue;

            totalRenderers++;
            bool tiene = false;

            foreach (var feature in datos.rendererFeatures)
                if (feature is ARBackgroundRendererFeature) tiene = true;

            if (!tiene) sinFeature++;
        }

        Linea(sb, totalRenderers > 0 && sinFeature == 0,
              "URP: los " + totalRenderers + " renderers tienen el AR Background Renderer Feature",
              "URP: " + sinFeature + " de " + totalRenderers + " renderers sin AR Background " +
              "Renderer Feature. Sin eso la camara de AR sale negra. Arreglalo con un click en " +
              "Project Settings > XR Plug-in Management > Project Validation > Fix All");

        // --- 3. la library
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(kRutaLibrary);
        Linea(sb, library != null && library.count >= 3,
              "Library: " + (library != null ? library.count : 0) + " imagenes en " + kRutaLibrary,
              "Library: falta o tiene menos de 3 imagenes -> corre Patrimonio AR > 1");

        // --- 4. el rig de AR en la escena
        var sesion = Object.FindFirstObjectByType<ARSession>();
        var manager = Object.FindFirstObjectByType<ARTrackedImageManager>();
        var spawner = Object.FindFirstObjectByType<ImageTrackingSpawner>();

        Linea(sb, sesion != null, "Escena: AR Session",
              "Escena: falta el AR Session -> click derecho en la jerarquia > XR > AR Session");

        Linea(sb, manager != null, "Escena: AR Tracked Image Manager",
              "Escena: falta el AR Tracked Image Manager -> corre Patrimonio AR > 2");

        if (manager != null)
        {
            var so = new SerializedObject(manager);
            var propMax = so.FindProperty("m_MaxNumberOfMovingImages");
            int max = propMax != null ? propMax.intValue : 0;

            Linea(sb, max >= 3,
                  "Tracking multiple: Max Number Of Moving Images = " + max,
                  "Tracking multiple: Max Number Of Moving Images = " + max +
                  ". Es el requisito del enunciado, tiene que ser 3 -> corre Patrimonio AR > 2");
        }

        // --- 5. el spawner cableado
        int pares = 0;
        if (spawner != null)
        {
            var so = new SerializedObject(spawner);
            var lista = so.FindProperty("pares");
            if (lista != null) pares = lista.arraySize;
        }

        Linea(sb, pares >= 3,
              "Spawner: " + pares + " lugares cableados",
              "Spawner: " + pares + " lugares cableados -> corre Patrimonio AR > 2");

        // --- 6. plataforma activa
        Linea(sb, EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
              "Plataforma activa: Android",
              "Plataforma activa: " + EditorUserBuildSettings.activeBuildTarget +
              ". Para compilar la APK hay que cambiarla a Android (el menu 4 lo ofrece).");

        sb.AppendLine();
        sb.AppendLine("Player Settings: " + kProducto + " / " + kCompany + " / " + kPaquete);

        return sb.ToString();
    }

    static void Linea(StringBuilder sb, bool ok, string textoOk, string textoMal)
    {
        sb.AppendLine((ok ? "[OK]  " : "[  ]  ") + (ok ? textoOk : textoMal));
    }

    static string LoadersDe(BuildTargetGroup grupo)
    {
        try
        {
            var porPlataforma = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(kRutaXR);
            var general = porPlataforma != null ? porPlataforma.SettingsForBuildTarget(grupo) : null;
            if (general == null || general.Manager == null) return "";

            var sb = new StringBuilder();
            foreach (var loader in general.Manager.activeLoaders)
                if (loader != null) sb.Append(loader.GetType().Name).Append(" ");

            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }

    // ================================================================ MENU 4
    [MenuItem("Patrimonio AR/4 - Compilar APK con mi nombre", false, 50)]
    public static void CompilarApk()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (EditorUtility.DisplayDialog("Plataforma",
                    "La plataforma activa no es Android. La cambio ahora?\n\n" +
                    "Cuando Unity termine de recargar, volve a ejecutar este menu.",
                    "Cambiar a Android", "Cancelar"))
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);
            return;
        }

        string nombreArchivo = "PatrimonioAR-" + kNombreAlumno.Replace(' ', '-');
        string ruta = EditorUtility.SaveFilePanel("Guardar APK", "", nombreArchivo, "apk");
        if (string.IsNullOrEmpty(ruta)) return;

        // Se compila SOLO la escena del prototipo: APK chica y sin escenas de prueba.
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { kRutaEscena },
            locationPathName = ruta,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var reporte = BuildPipeline.BuildPlayer(opciones);

        if (reporte != null && reporte.summary.result == BuildResult.Succeeded)
        {
            float mb = reporte.summary.totalSize / 1024f / 1024f;
            Debug.Log("[Patrimonio AR] APK compilada en " + ruta + " (" + mb.ToString("0.0") + " MB)");

            EditorUtility.DisplayDialog("Patrimonio AR",
                "APK compilada:\n" + ruta + "\n\n" +
                "Tamano: " + mb.ToString("0.0") + " MB\n\n" +
                (mb > 100f
                    ? "Pasa los 100 MB: hay que subirla a Drive con acceso publico y adjuntar un " +
                      ".txt con el link, tambien con tu nombre completo."
                    : "Entra en los 100 MB, se puede subir directo."), "Listo");

            EditorUtility.RevealInFinder(ruta);
        }
        else
        {
            Debug.LogError("[Patrimonio AR] La compilacion fallo. Mira el detalle en la consola.");
        }
    }

    // ================================================================ helpers
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
}
