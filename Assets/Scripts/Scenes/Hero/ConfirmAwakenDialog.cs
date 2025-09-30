/*
============================================================
ConfirmAwakenDialog.cs — Confirmación de despertar
------------------------------------------------------------
PROPÓSITO
- Confirmar consumo de recursos y aplicar awaken del héroe.

MÉTODOS (COMPLETA AQUÍ)
- Show(heroId)/Hide().
- OnConfirm(): aplica cambios en HeroProgress y refresca UI.
============================================================
*/


using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmAwakenDialog : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject root;     // contenedor del diálogo; si es null usa este GO
    [SerializeField] private TMP_Text titleText;  // opcional
    [SerializeField] private TMP_Text bodyText;   // opcional
    [SerializeField] private Button siBtn;        // botón SÍ / OK
    [SerializeField] private Button noBtn;        // botón NO / Cancelar

    private Action _onYes;
    private Action _onNo;

    void Awake()
    {
        if (!root) root = gameObject;
        root.SetActive(false); // oculto por defecto
    }

    /// <summary> API nueva (recomendada) </summary>
    public void Open(string title, string body, Action onYes, Action onNo = null)
    {
        _onYes = onYes;
        _onNo  = onNo;

        if (titleText) titleText.text = string.IsNullOrEmpty(title) ? "Confirmar" : title;
        if (bodyText)  bodyText.text  = body ?? "";

        if (siBtn)
        {
            siBtn.onClick.RemoveAllListeners();
            siBtn.onClick.AddListener(() =>
            {
                Close();
                _onYes?.Invoke();
            });
        }

        if (noBtn)
        {
            noBtn.onClick.RemoveAllListeners();
            noBtn.onClick.AddListener(() =>
            {
                Close();
                _onNo?.Invoke();
            });
        }

        root.SetActive(true);
    }

    /// <summary> API antigua (compatibilidad con tus scripts): delega en Open </summary>
    public void Show(string title, string body, Action onOk, Action onCancel = null)
    {
        Open(title, body, onOk, onCancel);
    }

    public void Close()
    {
        (root ? root : gameObject).SetActive(false);
    }
}
