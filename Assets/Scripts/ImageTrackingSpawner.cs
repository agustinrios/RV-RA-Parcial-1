using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Nucleo del prototipo: escucha al ARTrackedImageManager e instancia el diorama
/// correspondiente a cada postal detectada.
///
/// El prefab se crea como HIJO del ARTrackedImage, asi el diorama sigue a la postal
/// sin una sola linea de codigo que lo acompanie: lo mueve la jerarquia.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTrackingSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ParImagenPrefab
    {
        [Tooltip("Debe coincidir EXACTAMENTE con el Name de la imagen en la Reference Image Library")]
        public string nombreImagen;
        public GameObject prefab;
    }

    [SerializeField] private List<ParImagenPrefab> pares = new List<ParImagenPrefab>();

    private ARTrackedImageManager trackedImageManager;
    private readonly Dictionary<string, GameObject> instanciados = new Dictionary<string, GameObject>();
    private readonly HashSet<string> yaAvisadas = new HashSet<string>();

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    /// <summary>
    /// trackablesChanged es la API de AR Foundation 6. La vieja (trackedImagesChanged),
    /// que es la que aparece en casi todos los tutoriales, esta marcada como obsoleta.
    /// </summary>
    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // Postal detectada por primera vez
        foreach (var imagen in args.added)
            CrearOActualizar(imagen);

        // Postal que ya estaba y cambio de posicion o de estado de tracking
        foreach (var imagen in args.updated)
            CrearOActualizar(imagen);

        // Postal que el sistema dejo de rastrear por completo
        foreach (var par in args.removed)
        {
            var nombre = par.Value.referenceImage.name;
            if (string.IsNullOrEmpty(nombre)) continue;

            if (instanciados.TryGetValue(nombre, out var go) && go != null)
                go.SetActive(false);
        }
    }

    private void CrearOActualizar(ARTrackedImage imagen)
    {
        if (imagen == null) return;

        string nombre = imagen.referenceImage.name;

        // XR Simulation rastrea TODOS los SimulatedTrackedImage del entorno, incluso los que
        // no estan cargados en la Reference Image Library. Para esos, AR Foundation no puede
        // resolver la reference image y el nombre vuelve nulo. Hay que ignorarlos: si no, el
        // Dictionary tira ArgumentNullException, se corta el foreach y tampoco aparecen las
        // postales que si estan bien cargadas.
        if (string.IsNullOrEmpty(nombre)) return;

        // Si la instancia se destruyo (AR Foundation borra el ARTrackedImage, y con el a su
        // hijo, cuando el trackable se da de baja), se vuelve a crear.
        if (!instanciados.TryGetValue(nombre, out var instancia) || instancia == null)
        {
            var par = pares.Find(p => p.nombreImagen == nombre);

            if (par == null || par.prefab == null)
            {
                // Se avisa una sola vez por imagen: esto corre en cada frame de tracking.
                if (yaAvisadas.Add(nombre))
                    Debug.LogWarning($"[Patrimonio AR] No hay prefab asignado para la imagen '{nombre}'");
                return;
            }

            instancia = Instantiate(par.prefab, imagen.transform);
            instancia.transform.localPosition = Vector3.zero;
            instancia.transform.localRotation = Quaternion.identity;
            instanciados[nombre] = instancia;
        }

        // El diorama se muestra solo si el tracking es confiable. Se oculta, no se destruye:
        // tapas la postal y desaparece, la destapas y vuelve al instante sin reinstanciar.
        instancia.SetActive(imagen.trackingState == TrackingState.Tracking);
    }
}
