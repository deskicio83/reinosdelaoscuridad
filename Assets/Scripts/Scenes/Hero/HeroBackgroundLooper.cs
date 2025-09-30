/*
============================================================
HeroBackgroundLooper.cs — Efecto parallax/loop de fondo
------------------------------------------------------------
PROPÓSITO
- Mover/oscilar el fondo en HeroScene para dar vida.

MÉTODOS (COMPLETA AQUÍ)
- Update(): desplazamiento cíclico/por input.
============================================================
*/


using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeroBackgroundLooper : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RawImage raw;      // Asigna el RawImage del fondo

    [Header("Movimiento")]
    [Tooltip("Unidades UV por segundo. 0.05–0.12 suele ir bien.")]
    [SerializeField] private float speed = 0.08f;
    [Tooltip("Dirección del scroll. (1,0)=derecha, (-1,0)=izquierda, (0,1)=arriba, etc.")]
    [SerializeField] private Vector2 direction = new Vector2(1f, 0f);

    private void Reset()
    {
        raw = GetComponent<RawImage>();
    }

    private void Update()
    {
        if (raw == null || raw.texture == null) return;

        // Avanza el uvRect y envuélvelo con Repeat
        var uv = raw.uvRect;
        uv.x = Mathf.Repeat(uv.x + direction.x * speed * Time.unscaledDeltaTime, 1f);
        uv.y = Mathf.Repeat(uv.y + direction.y * speed * Time.unscaledDeltaTime, 1f);
        raw.uvRect = uv;
    }

    /// Llama a esto cuando cambies de fondo para reiniciar el offset.
    public void Restart()
    {
        if (raw == null) return;
        raw.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    /// (Opcional) Si cargas la textura por código, pasa por aquí para forzar WrapMode.
    public void SetTexture(Texture tex)
    {
        if (raw == null) return;
        raw.texture = tex;
        if (tex != null) tex.wrapMode = TextureWrapMode.Repeat;
        Restart();
    }
}
