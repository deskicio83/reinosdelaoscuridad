/*
============================================================
MusicManager.cs — Música de fondo/FX
------------------------------------------------------------
PROPÓSITO
- Controlar pistas, crossfades y ajustes globales.

MÉTODOS (COMPLETA AQUÍ)
- PlayBGM(string key), StopBGM(), SetVolume(float).
============================================================
*/

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private string currentMusicKey;

    private Dictionary<string, string> sceneToMusicKey = new Dictionary<string, string>()
    {
        { "BootScene", "Assets/Addressables/Audio/BootScene/Battle_Ready.mp3" },
        { "CombatScene", "Music_CombatScene" },
        { "GachaScene", "Music_GachaScene" }
        // Añade más entradas según tus escenas
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.5f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        if (!sceneToMusicKey.TryGetValue(sceneName, out string musicKey))
        {
            Debug.LogWarning($"🎵 No se ha definido música para la escena: {sceneName}");
            return;
        }

        if (musicKey == currentMusicKey && audioSource.isPlaying)
        {
            Debug.Log($"🎵 Ya se está reproduciendo la música: {musicKey}");
            return;
        }

        Addressables.LoadAssetAsync<AudioClip>(musicKey).Completed += OnMusicLoaded;
        currentMusicKey = musicKey;
    }

    private void OnMusicLoaded(AsyncOperationHandle<AudioClip> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"❌ No se pudo cargar la música: {currentMusicKey}");
            return;
        }

        AudioClip clip = handle.Result;

        if (clip == null)
        {
            Debug.LogError($"❌ El clip de música está vacío: {currentMusicKey}");
            return;
        }

        audioSource.clip = clip;
        audioSource.Play();
        Debug.Log($"🎵 Reproduciendo música: {clip.name}");
    }
}
