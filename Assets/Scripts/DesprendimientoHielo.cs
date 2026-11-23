using UnityEngine;

/// <summary>
/// Desprendimiento de hielo del glaciar: cada cierto intervalo un bloque del frente se
/// suelta, cae con gravedad real hasta la linea del agua, levanta una salpicadura y el
/// bloque vuelve a su lugar.
///
/// Es el detalle que explica el dato de la ficha ("avanza 2 m por dia"): el glaciar se
/// mueve, y lo que se ve del movimiento son los desprendimientos.
/// </summary>
public class DesprendimientoHielo : MonoBehaviour
{
    [Tooltip("Bloque del frente que se desprende.")]
    [SerializeField] private Transform bloque;

    [Tooltip("Esfera blanca de la salpicadura. Se agranda y se apaga al impactar.")]
    [SerializeField] private Transform salpicadura;

    [Tooltip("Y local de la linea del agua, en metros reales del diorama.")]
    [SerializeField] private float nivelAgua = 0f;

    [Tooltip("Segundos de espera entre un desprendimiento y el siguiente.")]
    [SerializeField] private float intervalo = 5f;

    [Tooltip("Gravedad en metros reales por segundo al cuadrado.")]
    [SerializeField] private float gravedad = 9.81f;

    [Tooltip("Tamano final de la salpicadura en metros reales.")]
    [SerializeField] private float tamanoSalpicadura = 18f;

    private Vector3 posicionOriginal;
    private Quaternion rotacionOriginal;
    private float espera;
    private float velocidad;
    private float tiempoSalpicadura = -1f;
    private bool cayendo;

    private void Awake()
    {
        if (bloque != null)
        {
            posicionOriginal = bloque.localPosition;
            rotacionOriginal = bloque.localRotation;
        }
    }

    private void OnEnable()
    {
        espera = intervalo * 0.5f;
        cayendo = false;
        velocidad = 0f;
        tiempoSalpicadura = -1f;

        if (bloque != null)
        {
            bloque.localPosition = posicionOriginal;
            bloque.localRotation = rotacionOriginal;
        }

        if (salpicadura != null)
            salpicadura.localScale = Vector3.zero;
    }

    private void Update()
    {
        AnimarSalpicadura();

        if (bloque == null) return;

        if (!cayendo)
        {
            espera -= Time.deltaTime;
            if (espera > 0f) return;

            cayendo = true;
            velocidad = 0f;
            return;
        }

        velocidad += gravedad * Time.deltaTime;

        var p = bloque.localPosition;
        p.y -= velocidad * Time.deltaTime;

        // Vuelca un poco mientras cae, como un tempano que se da vuelta.
        bloque.localRotation = rotacionOriginal * Quaternion.Euler(velocidad * 1.2f, 0f, velocidad * 0.6f);

        if (p.y <= nivelAgua)
        {
            // Impacto: salpicadura y vuelta al frente del glaciar.
            if (salpicadura != null)
            {
                salpicadura.localPosition = new Vector3(p.x, nivelAgua, p.z);
                tiempoSalpicadura = 0f;
            }

            bloque.localPosition = posicionOriginal;
            bloque.localRotation = rotacionOriginal;
            cayendo = false;
            espera = intervalo;
            return;
        }

        bloque.localPosition = p;
    }

    private void AnimarSalpicadura()
    {
        if (salpicadura == null || tiempoSalpicadura < 0f) return;

        const float duracion = 1.1f;
        tiempoSalpicadura += Time.deltaTime;

        float t = Mathf.Clamp01(tiempoSalpicadura / duracion);
        float crecer = 1f - Mathf.Pow(1f - t, 3f);
        float apagar = 1f - t;

        salpicadura.localScale = Vector3.one * tamanoSalpicadura * crecer * apagar;

        if (t >= 1f)
        {
            salpicadura.localScale = Vector3.zero;
            tiempoSalpicadura = -1f;
        }
    }
}
