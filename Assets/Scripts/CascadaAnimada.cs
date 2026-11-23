using UnityEngine;

/// <summary>
/// Hace caer en bucle a todos los hijos de este objeto, que son las vetas de agua de la
/// cascada. Trabaja en coordenadas locales y en metros reales del diorama: como el diorama
/// entero esta escalado, la velocidad que se declara aca es la velocidad real del agua.
/// </summary>
public class CascadaAnimada : MonoBehaviour
{
    [Tooltip("Altura de la caida en metros reales. Al llegar abajo, la veta vuelve arriba.")]
    [SerializeField] private float alturaCaida = 80f;

    [Tooltip("Velocidad de caida en metros reales por segundo.")]
    [SerializeField] private float velocidad = 18f;

    private Transform[] vetas;
    private float[] yInicial;

    private void Awake()
    {
        int cantidad = transform.childCount;
        vetas = new Transform[cantidad];
        yInicial = new float[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            vetas[i] = transform.GetChild(i);
            yInicial[i] = vetas[i].localPosition.y;

            // Cada veta arranca en un punto distinto de la caida para que no caigan en bloque.
            float desfase = alturaCaida * (i / Mathf.Max(1f, (float)cantidad));
            var p = vetas[i].localPosition;
            p.y = yInicial[i] - desfase;
            vetas[i].localPosition = p;
        }
    }

    private void Update()
    {
        if (vetas == null) return;

        float paso = velocidad * Time.deltaTime;

        for (int i = 0; i < vetas.Length; i++)
        {
            if (vetas[i] == null) continue;

            var p = vetas[i].localPosition;
            p.y -= paso;

            // Al pasarse del largo de la caida, vuelve al borde de arriba.
            if (p.y < yInicial[i] - alturaCaida)
                p.y += alturaCaida;

            vetas[i].localPosition = p;
        }
    }
}
