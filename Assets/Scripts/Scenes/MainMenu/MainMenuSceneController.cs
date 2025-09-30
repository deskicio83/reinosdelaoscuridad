/*
============================================================
MainMenuSceneController.cs — Orquestación del menú principal
------------------------------------------------------------
PROPÓSITO
- Construir mundo UI (WorldRoot), fondo, y manejar navegación.

MÉTODOS (COMPLETA AQUÍ)
- Awake/Start(): SceneUICommon.EnsureWorldSceneStructure(...).
- Hook a WorldPanZoom, ClickablePoi, HUDController.
============================================================
*/

using UnityEngine;
using UnityEngine.UI;

public class MainMenuSceneController : MonoBehaviour
{
    [Header("World")]
    public Transform worldRoot;                // Contenedor del mundo (por si animas props/NPCs)
    public Camera worldCamera;                 // Cámara ortográfica
    public SpriteRenderer backgroundSprite;    // Background grande (limita el movimiento)
    public WorldPanZoom panZoom;               // Componente WorldPanZoom

    [Header("HUD / Navegación")]
    public Button btnHeroes;
    public Button btnGacha;
    public Button btnShop;

    [Header("Escenas")]
    public string heroesSceneName = "HeroScene";
    public string gachaSceneName  = "GachaScene";
    public string shopSceneName   = "ShopScene";

    private void Start()
    {
        // Pan/Zoom
        if (!worldCamera) worldCamera = Camera.main;
        if (panZoom == null && worldCamera != null)
            panZoom = worldCamera.GetComponent<WorldPanZoom>();
        if (panZoom != null)
        {
            panZoom.worldCamera = worldCamera;
            panZoom.backgroundSprite = backgroundSprite;
            panZoom.RecalculateLimitsImmediate();
        }

        // Navegación (SceneLoader es estático)
        if (btnHeroes != null) { btnHeroes.onClick.RemoveAllListeners(); btnHeroes.onClick.AddListener(() => SceneLoader.LoadScene(heroesSceneName)); }
        if (btnGacha  != null) { btnGacha.onClick.RemoveAllListeners();  btnGacha.onClick.AddListener(() => SceneLoader.LoadScene(gachaSceneName)); }
        if (btnShop   != null) { btnShop.onClick.RemoveAllListeners();   btnShop.onClick.AddListener(() => SceneLoader.LoadScene(shopSceneName)); }
    }
}
