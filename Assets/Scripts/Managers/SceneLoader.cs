/*
============================================================
SceneLoader.cs — Carga y transición de escenas
------------------------------------------------------------
PROPÓSITO
- API estática para cargar escenas con overlay/fade.

MÉTODOS (COMPLETA AQUÍ)
- Load(string sceneName): muestra LoadingOverlayController.
- LoadAdditive(string).
============================================================
*/

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void LoadScene(string addressableSceneKey)
    {
        Debug.Log($"🔁 Cargando escena: {addressableSceneKey}...");

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeIn(() =>
            {
                var handle = Addressables.LoadSceneAsync(addressableSceneKey, LoadSceneMode.Single);
                handle.Completed += op =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log($"✅ Escena cargada: {addressableSceneKey}");
                    }
                    else
                    {
                        Debug.LogError($"❌ Error al cargar escena: {addressableSceneKey}");
                    }
                    SceneFader.Instance.FadeOut();
                };
            });
        }
        else
        {
            var handle = Addressables.LoadSceneAsync(addressableSceneKey, LoadSceneMode.Single);
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    Debug.Log($"✅ Escena cargada (sin fade): {addressableSceneKey}");
                else
                    Debug.LogError($"❌ Error al cargar escena (sin fade): {addressableSceneKey}");
            };
        }
    }

    public static void LoadSceneAdditive(string addressableSceneKey)
    {
        Debug.Log($"➕ Cargando aditiva: {addressableSceneKey}...");
        Addressables.LoadSceneAsync(addressableSceneKey, LoadSceneMode.Additive).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                Debug.Log($"✅ Aditiva cargada: {addressableSceneKey}");
            else
                Debug.LogError($"❌ Error aditiva: {addressableSceneKey}");
        };
    }
}
            