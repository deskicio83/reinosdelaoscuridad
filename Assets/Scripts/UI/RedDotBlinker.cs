using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class RedDotBlinker : MonoBehaviour
{
    [Header("Sprites (en orden)")]
    public Sprite openSprite;
    public Sprite halfSprite;
    public Sprite closedSprite;

    [Header("Timing (segundos)")]
    [Tooltip("Tiempo con el ojo abierto antes de otro parpadeo.")]
    public Vector2 idleOpenHold = new Vector2(1.0f, 2.25f);
    [Tooltip("Duración del estado medio (ida/vuelta).")]
    public float halfTime = 0.06f;
    [Tooltip("Tiempo que permanece completamente cerrado.")]
    public float closedTime = 0.08f;

    [Header("Opciones")]
    [Tooltip("Arranca el parpadeo automáticamente al activarse el GO.")]
    public bool autoPlay = true;
    [Tooltip("Desfase aleatorio para no sincronizar todos los red dots.")]
    public bool randomizePhase = true;
    [Tooltip("Ajustar tamaño al sprite del ojo (opcional).")]
    public bool setNativeSize = false;

    private Image img;
    private Coroutine loop;

    void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = false;

        // Evita el frame en blanco: el Image no dibuja hasta que lo preparamos.
        img.enabled = false;
    }

    void OnEnable()
    {
        // Coloca el sprite visible ANTES del primer frame y habilita el Image.
        Prewarm();

        if (autoPlay)
            StartLoop();
    }

    void OnDisable()
    {
        StopLoop();
    }

    /// <summary>
    /// Deja el ojo preparado (sprite abierto puesto y Image habilitado) sin parpadear.
    /// Llamado automáticamente en OnEnable. Útil si quieres forzarlo manualmente.
    /// </summary>
    public void Prewarm()
    {
        if (!img) img = GetComponent<Image>();

        if (openSprite != null)
        {
            img.sprite = openSprite;
            if (setNativeSize) img.SetNativeSize();
            img.enabled = true; // recién ahora dejamos que se dibuje
        }
        else
        {
            // Si no hay sprite asignado, mejor no dibujar nada.
            img.enabled = false;
        }
    }

    public void StartLoop()
    {
        if (loop != null) return;
        loop = StartCoroutine(BlinkLoop());
    }

    public void StopLoop()
    {
        if (loop == null) return;
        StopCoroutine(loop);
        loop = null;
    }

    /// Fuerza un parpadeo puntual (p. ej. para enfatizar)
    public void PulseOnce()
    {
        if (!isActiveAndEnabled) return;
        if (loop != null) StopLoop();
        loop = StartCoroutine(BlinkOnceThenResume());
    }

    IEnumerator BlinkOnceThenResume()
    {
        yield return StartCoroutine(BlinkOnce());
        loop = StartCoroutine(BlinkLoop());
    }

    IEnumerator BlinkLoop()
    {
        if (randomizePhase)
            yield return new WaitForSeconds(Random.Range(0f, 0.75f));

        while (true)
        {
            // Ojo abierto un rato aleatorio
            yield return new WaitForSeconds(Random.Range(idleOpenHold.x, idleOpenHold.y));

            // Parpadeo
            yield return StartCoroutine(BlinkOnce());
        }
    }

    IEnumerator BlinkOnce()
    {
        if (halfSprite)
        {
            img.sprite = halfSprite;
            if (setNativeSize) img.SetNativeSize();
            yield return new WaitForSeconds(Mathf.Max(0.01f, halfTime));
        }

        if (closedSprite)
        {
            img.sprite = closedSprite;
            if (setNativeSize) img.SetNativeSize();
            yield return new WaitForSeconds(Mathf.Max(0.01f, closedTime));
        }

        if (halfSprite)
        {
            img.sprite = halfSprite;
            if (setNativeSize) img.SetNativeSize();
            yield return new WaitForSeconds(Mathf.Max(0.01f, halfTime));
        }

        img.sprite = openSprite;
        if (setNativeSize) img.SetNativeSize();
    }
}
