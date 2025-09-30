/*
============================================================
UIModalBlocker.cs — Bloqueador modal de clicks (singleton)
------------------------------------------------------------
PROPÓSITO
- Capa transparente que captura el primer click “fuera” para cerrar paneles.

USO
- UIModalBlocker.Instance.Activate(owner, onClickOutside) para mostrar.
- UIModalBlocker.Instance.Release(owner) para ocultar.

MÉTODOS
- Awake(): asegura singleton, añade Image (raycastTarget=true), arranca oculto.
- Activate(Component owner, Action onClickOutside): muestra y guarda callback.
- Release(Component owner): oculta si lo libera el mismo owner.
- OnPointerDown(PointerEventData): ejecuta y limpia el callback.

NOTAS
- No reparenta: se mantiene bajo el Canvas. Orden con SetAsLastSibling().
============================================================
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-50)]
public class UIModalBlocker : MonoBehaviour, IPointerDownHandler
{
    public static UIModalBlocker Instance { get; private set; }

    private Action _onClickOutside;
    private Component _owner;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var img = GetComponent<Image>();
        if (img == null) img = gameObject.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0f); // invisible
        img.raycastTarget = true;

        gameObject.SetActive(false);
    }

    /// <summary> Activa el bloqueador para un owner; cerrará al primer PointerDown. </summary>
    public void Activate(Component owner, Action onClickOutside)
    {
        _owner = owner;
        _onClickOutside = onClickOutside;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    /// <summary> Desactiva si el que la libera es el owner. </summary>
    public void Release(Component owner)
    {
        if (_owner == owner)
        {
            _owner = null;
            _onClickOutside = null;
            gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var cb = _onClickOutside;
        _onClickOutside = null; // evita dobles/rehabilitar
        cb?.Invoke();
    }
}
