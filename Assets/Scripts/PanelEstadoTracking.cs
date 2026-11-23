using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// HUD de diagnostico: lista en pantalla todas las postales que el ARTrackedImageManager
/// tiene registradas y su estado de tracking.
///
/// Es lo que vuelve DEMOSTRABLE el seguimiento multiple que pide el enunciado: sin el HUD,
/// que la app soporte varias imagenes a la vez queda en la palabra del alumno; con el HUD
/// se lee "Postales rastreadas: 3 / 3" en pantalla.
/// </summary>
public class PanelEstadoTracking : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager manager;
    [SerializeField] private TMP_Text texto;
    [SerializeField] private string mensajeSinImagenes = "Apunta a una postal";

    private readonly StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        if (manager == null) manager = FindFirstObjectByType<ARTrackedImageManager>();
        if (texto == null) texto = GetComponentInChildren<TMP_Text>();
    }

    private void Update()
    {
        if (texto == null) return;

        if (manager == null)
        {
            manager = FindFirstObjectByType<ARTrackedImageManager>();
            if (manager == null)
            {
                texto.text = "Sin ARTrackedImageManager en la escena";
                return;
            }
        }

        sb.Clear();
        int rastreando = 0;
        int total = 0;

        foreach (var imagen in manager.trackables)
        {
            string nombre = imagen.referenceImage.name;
            if (string.IsNullOrEmpty(nombre)) continue;  // cartas del entorno que no estan en la library

            total++;
            bool estable = imagen.trackingState == TrackingState.Tracking;
            if (estable) rastreando++;

            sb.Append(estable ? "<color=#8BE78B>" : "<color=#E7C98B>");
            sb.Append("- ");
            sb.Append(nombre.Replace('_', ' '));
            sb.Append("  [");
            sb.Append(imagen.trackingState);
            sb.Append("]</color>\n");
        }

        if (total == 0)
        {
            texto.text = mensajeSinImagenes;
            return;
        }

        texto.text = "Postales rastreadas: " + rastreando + " / " + total + "\n" + sb;
    }
}
