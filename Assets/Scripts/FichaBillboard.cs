using UnityEngine;

/// <summary>
/// Mantiene la ficha de datos del lugar orientada hacia la camara de AR.
/// Gira unicamente alrededor de la normal de la postal (el eje Y del ARTrackedImage),
/// para que el texto nunca se incline ni quede cabeza abajo.
/// </summary>
public class FichaBillboard : MonoBehaviour
{
    [Tooltip("Si el texto apareciera espejado, activar esta casilla.")]
    [SerializeField] private bool invertirFrente = false;

    [Tooltip("Transform que define el eje 'arriba'. Si queda vacio usa el padre (la postal).")]
    [SerializeField] private Transform referenciaArriba;

    private Camera camara;

    private void OnEnable()
    {
        if (referenciaArriba == null)
            referenciaArriba = transform.parent;

        BuscarCamara();
    }

    private void BuscarCamara()
    {
        camara = Camera.main;
        if (camara == null)
            camara = FindFirstObjectByType<Camera>();
    }

    private void LateUpdate()
    {
        if (camara == null)
        {
            BuscarCamara();
            if (camara == null) return;
        }

        Vector3 arriba = referenciaArriba != null ? referenciaArriba.up : Vector3.up;

        // El frente visible de un quad / de un texto de Unity es su cara -Z, por eso el +Z
        // tiene que apuntar en direccion CONTRARIA a la camara. El transform.LookAt(camara)
        // que aparece en todos los tutoriales deja el texto espejado.
        Vector3 direccion = transform.position - camara.transform.position;
        if (invertirFrente) direccion = -direccion;

        // Proyectar sobre el plano de la postal: asi la ficha no se tumba cuando la mirás
        // desde arriba, solo gira sobre si misma.
        direccion = Vector3.ProjectOnPlane(direccion, arriba);
        if (direccion.sqrMagnitude < 0.000001f) return;

        transform.rotation = Quaternion.LookRotation(direccion.normalized, arriba);
    }
}
