/*
============================================================
BtnHeroesHandler.cs — Encaminador de clics a la escena de héroes
------------------------------------------------------------
PROPÓSITO
- Centralizar acciones de botones relacionados con navegación a HeroScene
  u operaciones rápidas sobre héroes desde UI superior.

USO
- Asignar el script a botones del HUD/menú.
- Conectar onClick a métodos públicos expuestos.

RESPONSABILIDADES
- Invocar SceneLoader o HeroSceneController según la acción solicitada.

DEPENDENCIAS
- SceneLoader, HeroSceneController, GameDataManager (si consulta estado).

REFERENCIAS (INSPECTOR)
- Botones a asociar (si se hace por script).
- Parámetros de navegación (si aplica).

EVENTOS/SEÑALES
- No define eventos propios.

MÉTODOS (COMPLETA AQUÍ)
- GoToHeroes(): Carga/activa la escena de héroes.
- GoToInventory(): Abre panel de equipamiento.
- GoToAwaken(): Abre panel de despertar.
============================================================
*/


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class BtnHeroesHandler : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform container;
    [SerializeField] private AssetReference buttonPrefabReference;

    private readonly List<GameObject> spawnedButtons = new();

    void Start()
    {
        LoadButtons();
    }

    private void LoadButtons()
    {
        ClearButtons();

        if (buttonPrefabReference == null)
        {
            Debug.LogError("[BtnHeroesHandler] Prefab reference is null.");
            return;
        }

        buttonPrefabReference.LoadAssetAsync<GameObject>().Completed += OnButtonPrefabLoaded;
    }

    private void OnButtonPrefabLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[BtnHeroesHandler] Failed to load button prefab.");
            return;
        }

        // Ejemplo: instanciamos 5 botones
        for (int i = 0; i < 5; i++)
        {
            var button = Instantiate(handle.Result, container);
            button.name = $"HeroButton_{i + 1}";
            spawnedButtons.Add(button);

            var btnComp = button.GetComponent<Button>();
            int heroIndex = i;
            if (btnComp != null)
            {
                btnComp.onClick.AddListener(() => OnHeroButtonClicked(heroIndex));
            }
        }
    }

    private void ClearButtons()
    {
        foreach (var go in spawnedButtons)
        {
            if (go != null) Destroy(go);
        }
        spawnedButtons.Clear();
    }

    private void OnHeroButtonClicked(int heroIndex)
    {
        Debug.Log($"[BtnHeroesHandler] Hero button clicked: {heroIndex}");
        // Aquí podrás llamar a HeroScene o abrir detalles del héroe
    }
}
