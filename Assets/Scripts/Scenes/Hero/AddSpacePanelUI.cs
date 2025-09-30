/*
============================================================
AddSpacePanelUI.cs — UI para comprar huecos de héroes
------------------------------------------------------------
PROPÓSITO
- Mostrar precio y confirmar ampliación de `maxHeroSpaces`.

MÉTODOS (COMPLETA AQUÍ)
- Show()/Hide().
- OnConfirm(): PlayerData.maxHeroSpaces++ y guardado.
============================================================
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AddSpacePanelUI : MonoBehaviour
{
    [Header("Referencias de recurso del jugador")]
    public TextMeshProUGUI oroNegroText;
    public TextMeshProUGUI caosiferaText;

    [Header("Botones de compra")]
    public Button buyOroNegroBtn;
    public Button buyCaosiferaBtn;
    public Button closeBtn;

    [Header("Costes y cantidad")]
    public int oroNegroCost = 200;
    public int caosiferaCost = 15;
    public int slotsToAdd = 5;

    [Header("UI Coste en botones")]
    public TextMeshProUGUI oroNegroCostText;
    public TextMeshProUGUI caosiferaCostText;


    [Header("Panel de error opcional")]
    public GameObject errorPanel; // opcional
    public TextMeshProUGUI errorText; // opcional

    private PlayerData playerData;
    private HeroSceneController heroSceneController;

    public void Init(PlayerData data, HeroSceneController sceneCtrl)
    {
        playerData = data;
        heroSceneController = sceneCtrl;
        RefreshCurrency();

        // Mostramos el coste en los botones
        if (oroNegroCostText != null)
            oroNegroCostText.text = oroNegroCost.ToString();

        if (caosiferaCostText != null)
            caosiferaCostText.text = caosiferaCost.ToString();

        // Los iconos puedes asignarlos en el inspector, o aquí por código si tienes sprites cargados.

        buyOroNegroBtn.onClick.RemoveAllListeners();
        buyCaosiferaBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.RemoveAllListeners();

        buyOroNegroBtn.onClick.AddListener(TryBuyWithOroNegro);
        buyCaosiferaBtn.onClick.AddListener(TryBuyWithCaosifera);
        closeBtn.onClick.AddListener(ClosePanel);

        // Oculta mensajes de error previos al abrir
        if (errorPanel != null)
            errorPanel.SetActive(false);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        if (heroSceneController != null && heroSceneController.uiBlockerPanel != null)
            heroSceneController.uiBlockerPanel.SetActive(false);

        if (errorPanel != null)
            errorPanel.SetActive(false);
    }

    public void RefreshCurrency()
    {
        if (oroNegroText != null)
            oroNegroText.text = playerData != null ? playerData.oroNegro.ToString() : "-";
        else
            Debug.LogWarning("[AddSpacePanelUI] oroNegroText no está asignado en el Inspector.");

        if (caosiferaText != null)
            caosiferaText.text = playerData != null ? playerData.caosifera.ToString() : "-";
        else
            Debug.LogWarning("[AddSpacePanelUI] caosiferaText no está asignado en el Inspector.");

        if (playerData == null)
            Debug.LogWarning("[AddSpacePanelUI] playerData no está asignado en RefreshCurrency.");
    }

    void TryBuyWithOroNegro()
    {
        if (playerData.TrySpendOroNegro(oroNegroCost))
        {
            playerData.AddHeroSpaces(slotsToAdd);

            // Refresca el contador de espacios y el grid de héroes
            heroSceneController.UpdateHeroCountText();
            if (heroSceneController.heroGridController != null)
                heroSceneController.heroGridController.SetHeroes(playerData.heroes, playerData.maxHeroSpaces);

            RefreshCurrency();
            ClosePanel();
        }
        else
        {
            ShowError("No tienes suficiente Oro Negro.");
        }
    }

    void TryBuyWithCaosifera()
    {
        if (playerData.TrySpendCaosifera(caosiferaCost))
        {
            playerData.AddHeroSpaces(slotsToAdd);

            // Refresca el contador de espacios y el grid de héroes
            heroSceneController.UpdateHeroCountText();
            if (heroSceneController.heroGridController != null)
                heroSceneController.heroGridController.SetHeroes(playerData.heroes, playerData.maxHeroSpaces);

            RefreshCurrency();
            ClosePanel();
        }
        else
        {
            ShowError("No tienes suficiente Caosífera.");
        }
    }

    // Mostrar error visual si lo necesitas, o simplemente usa Debug.Log
    void ShowError(string msg)
    {
        if (errorPanel != null && errorText != null)
        {
            errorText.text = msg;
            errorPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[AddSpacePanelUI] " + msg);
        }
    }
}
