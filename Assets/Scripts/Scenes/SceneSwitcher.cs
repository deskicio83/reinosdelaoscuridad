/*
============================================================
SceneSwitcher.cs — “Router” simple de escenas desde UI
------------------------------------------------------------
PROPÓSITO
- Enlazar botones a nombres de escenas declarados.

MÉTODOS (COMPLETA AQUÍ)
- SwitchTo(string sceneName), SwitchToMainMenu(), etc.
============================================================
*/

using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;

    public void LoadConfiguredScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneLoader.LoadScene(sceneToLoad);
    }

    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneLoader.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        SceneLoader.LoadScene("MainMenuScene");
    }

    public void LoadHeroScene()
    {
        SceneLoader.LoadScene("HeroScene");
    }

    public void LoadBoot()
    {
        SceneLoader.LoadScene("BootScene");
    }

    public void LoadSplash()
    {
        SceneLoader.LoadScene("SplashScene");
    }
}
