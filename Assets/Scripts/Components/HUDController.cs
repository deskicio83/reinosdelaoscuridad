/*
============================================================
HUDController.cs — Construcción/actualización del HUD superior
------------------------------------------------------------
PROPÓSITO
- Pintar recursos del jugador (oro, gemas, energía...), botones rápidos
  y vínculos a paneles.

USO
- Vivir en todas las escenas jugables; actualiza al cambiar PlayerData.

DEPENDENCIAS
- PlayerResourcesManager, GameDataManager, SceneUICommon.

MÉTODOS (COMPLETA AQUÍ)
- Awake/Start: Resolución de fuentes/iconos (Addressables).
- Refresh(): Lee PlayerData y actualiza textos/íconos.
- OnResourceChanged(...): Listener si PlayerResourcesManager emite eventos.
============================================================
*/


using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDController : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Text energiaText;
    public Text monedasText;
    public Text caosiferaText;
    public Text energiaTimerText = null; // (Opcional: puedes ponerlo null si no quieres temporizador)

    private bool energiaTimerVisible = false;
    private float energiaCooldown = 300f; // 5 minutos por energía (ajusta según tu lógica real)
    private float energiaTimer = 0f;

    private void Awake()
    {
        if (energiaText != null)
        {
            Button btn = energiaText.GetComponent<Button>();
            if (btn == null) btn = energiaText.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(ToggleEnergiaTimer);
        }
    }

    private void Start()
    {
        PlayerResourcesManager.OnDataLoaded += UpdateHUD;
        PlayerResourcesManager.OnDataChanged += UpdateHUD;

        energiaTimer = energiaCooldown;
        UpdateHUD();

        if (energiaTimerText != null)
            energiaTimerText.gameObject.SetActive(false); // Oculta por defecto
    }

    private void OnDestroy()
    {
        PlayerResourcesManager.OnDataLoaded -= UpdateHUD;
        PlayerResourcesManager.OnDataChanged -= UpdateHUD;
    }

    private void Update()
    {
        if (energiaTimerVisible && energiaTimerText != null)
        {
            energiaTimer -= Time.deltaTime;
            if (energiaTimer < 0f) energiaTimer = energiaCooldown;

            var data = PlayerResourcesManager.Data;
            if (data != null && data.energia >= data.energiaMax)
            {
                energiaTimerText.text = "Lleno";
            }
            else
            {
                TimeSpan t = TimeSpan.FromSeconds(energiaTimer);
                energiaTimerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
        }
    }

    public void ToggleEnergiaTimer()
    {
        energiaTimerVisible = !energiaTimerVisible;
        if (energiaTimerText != null)
            energiaTimerText.gameObject.SetActive(energiaTimerVisible);
    }

    public void UpdateHUD()
    {
        var data = PlayerResourcesManager.Data;
        if (data == null) return;

        if (energiaText != null)
            energiaText.text = $"{data.energia}/{data.energiaMax}";

        if (monedasText != null)
            monedasText.text = data.oroNegro.ToString();

        if (caosiferaText != null)
            caosiferaText.text = data.caosifera.ToString();
    }
}
