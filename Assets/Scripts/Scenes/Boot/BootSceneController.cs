/*
============================================================
BootSceneController.cs — Arranque inicial
------------------------------------------------------------
PROPÓSITO
- Construir UI base, EnsureEssentials, saltar a Splash/MainMenu.

MÉTODOS (COMPLETA AQUÍ)
- Start/Coroutine: carga catálogos, player data, música.
============================================================
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.Collections.Generic;

public class BootSceneController : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button loginButton;
    [SerializeField] private Text loginButtonText;
    // Dentro de BootSceneController
    public void GoToMainMenu()
    {
        SceneLoader.LoadScene("MainMenuScene");
    }

    public void GoToHeroScene()
    {
        SceneLoader.LoadScene("HeroScene");
    }

    private IEnumerator Start()
    {
        // Forzar orientación horizontal
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        if (loginButtonText != null)
            loginButtonText.text = "Entrar";

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);

        // Asegúrate de que HeroCatalogManager está en la escena
        var catalogManager = FindFirstObjectByType<HeroCatalogManager>();
        if (catalogManager != null)
        {
            Debug.Log("[BootSceneController] HeroCatalogManager encontrado en la escena.");
            // La carga del catálogo ocurre automáticamente en Awake del HeroCatalogManager
        }
        else
        {
            Debug.LogError("[BootSceneController] ❌ No se encontró HeroCatalogManager en la escena.");
        }

        yield return null;
    }

    private void OnLoginClicked()
    {
        // Sonido de click
        Addressables.LoadAssetAsync<AudioClip>("Assets/Addressables/Audio/Common/click_button.mp3").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                AudioSource.PlayClipAtPoint(handle.Result, Vector3.zero);
        };

        // Navega al menú principal
        //SceneLoader.LoadScene("MainMenuScene");
        SceneFader.Instance.FadeToScene("MainMenuScene");
    }
}
