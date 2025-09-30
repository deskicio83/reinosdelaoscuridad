/*
============================================================
AutoEquipConfirmPanelUI.cs — Modal de confirmación de auto-equip
------------------------------------------------------------
PROPÓSITO
- Mostrar resumen “N piezas a equipar” y aplicar la propuesta.

USO
- Show(heroId, proposal, onApplied): abre modal y, al confirmar, delega en servicio.

REFERENCIAS (INSPECTOR)
- root (panel), txtSummary, btnConfirm, btnCancel.

EVENTOS
- public event Action Closed; (notifica cierre)

MÉTODOS
- Awake(): wire de botones y arranque oculto.
- Show(string heroId, Dictionary<string,GearInstance> proposal, Action onApplied=null):
  prepara resumen, anima aparición con UIAnimator, habilita confirm si hay cambios.
- Hide(): oculta, dispara Closed, y anima desaparición.
- OnConfirmClicked()/OnCancelClicked(): rutas de cierre.
- ApplyProposal():
  ▶ Usa GearEquipService.ApplyProposal(...) (guarda y emite evento).
  ▶ *Refrescos UI* ahora los realiza GearUIRefreshManager.
============================================================
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AutoEquipConfirmPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;   // Raíz del modal (objeto del panel)
    [SerializeField] private TMP_Text txtSummary;
    [SerializeField] private Button btnConfirm; // Botón Confirmar
    [SerializeField] private Button btnCancel;  // Botón Cancelar

    private string _heroId;
    private Dictionary<string, GearInstance> _proposal;
    private Action _onApplied;

    // Evento para notificar cierre (Confirm o Cancel) al padre (PanelEquiparController)
    public event Action Closed;

    // Exponer la raíz para que el padre pueda ordenarlo si hace falta
    public GameObject Root => root != null ? root : gameObject;

    private void Awake()
    {
        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(OnConfirmClicked);
        }
        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(OnCancelClicked);
        }

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Show(string heroId, Dictionary<string, GearInstance> proposal, Action onApplied = null)
    {
        _heroId = heroId;
        _proposal = proposal;
        _onApplied = onApplied;

        var rootGO = Root;
        if (!rootGO.activeSelf) rootGO.SetActive(true);
        rootGO.transform.SetAsLastSibling(); // siempre encima
        // ANIMACIÓN
        UIAnimator.Appear(rootGO);

        int n = (_proposal != null) ? _proposal.Count : 0;
        if (txtSummary != null)
            txtSummary.text = n > 0 ? $"Se equiparán {n} piezas. ¿Confirmar?" : "No hay mejoras encontradas.";
        if (btnConfirm != null) btnConfirm.interactable = n > 0;
        if (btnCancel != null) btnCancel.interactable = true;
    }

    public void Hide()
    {
        var rootGO = Root;
        if (rootGO.activeSelf) rootGO.SetActive(false);
        Closed?.Invoke();
        // ANIMACIÓN
        UIAnimator.Disappear(rootGO);
    }

    private void OnConfirmClicked()
    {
        ApplyProposal();
        Hide();
    }

    private void OnCancelClicked()
    {
        Hide();
    }

    private void ApplyProposal()
    {
        if (string.IsNullOrEmpty(_heroId) || _proposal == null || _proposal.Count == 0)
            return;

        // APLICAR vía servicio (guarda + evento)
        GearEquipService.ApplyProposal(_heroId, _proposal, save: true, raiseEvents: true);

        // Ya NO hacemos refrescos locales aquí (los hace GearUIRefreshManager).
        _onApplied?.Invoke();
    }

}
