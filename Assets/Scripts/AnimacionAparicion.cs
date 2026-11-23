using UnityEngine;

/// <summary>
/// Escala el diorama desde cero hasta su tamano final cada vez que se activa.
/// Da feedback visual del momento exacto en que la postal pasa a estado Tracking.
/// Corre en OnEnable, asi que se redispara CADA vez que la postal vuelve a detectarse,
/// no solo la primera.
/// </summary>
public class AnimacionAparicion : MonoBehaviour
{
    [SerializeField] private float duracion = 0.45f;
    [SerializeField] private float rebote = 0.10f;

    private Vector3 escalaFinal = Vector3.one;
    private float tiempo;
    private bool animando;

    private void Awake()
    {
        escalaFinal = transform.localScale;
    }

    private void OnEnable()
    {
        tiempo = 0f;
        animando = true;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (!animando) return;

        tiempo += Time.deltaTime;
        float t = duracion <= 0f ? 1f : Mathf.Clamp01(tiempo / duracion);

        // Ease out cubico mas un rebote que se apaga sobre el final.
        float suave = 1f - Mathf.Pow(1f - t, 3f);
        float extra = Mathf.Sin(t * Mathf.PI) * rebote * (1f - t);
        transform.localScale = escalaFinal * (suave + extra);

        if (t >= 1f)
        {
            transform.localScale = escalaFinal;
            animando = false;
        }
    }
}
