#if UNITY_EDITOR
/// S19fix_Test — validaciones automáticas del flujo MVP.
/// Borrar este archivo antes del commit de producción.
/// Menú: Tools → Reino Oscuridad → S19fix Test Scenes
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.Boot;
using ReinoOscuridad.UI.Campaign;

public static class S19fix_Test
{
    [MenuItem("Tools/Reino Oscuridad/S19fix Test Scenes")]
    public static void RunAll()
    {
        int passed = 0, failed = 0;

        void Check(string desc, bool cond)
        {
            if (cond) { Debug.Log($"[S19fix] PASS — {desc}"); passed++; }
            else       { Debug.LogError($"[S19fix] FAIL — {desc}"); failed++; }
        }

        // ── CHECK 1: BootCanvas tiene InputSystemUIInputModule ────────────────
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[S19fix] Abortado por el usuario al guardar la escena.");
            return;
        }

        const string BOOT_PATH = "Assets/Scenes/BootScene.unity";
        const string MAIN_PATH = "Assets/Scenes/MainMenuScene.unity";
        const string CAMP_PATH = "Assets/Scenes/CampaignScene.unity";

        // ── BootScene ─────────────────────────────────────────────────────────
        if (!System.IO.File.Exists(BOOT_PATH))
        {
            Debug.LogError("[S19fix] BootScene.unity no encontrada — ejecuta primero Tools → 9. Setup BootScene");
        }
        else
        {
            EditorSceneManager.OpenScene(BOOT_PATH, OpenSceneMode.Single);

            var es = Object.FindFirstObjectByType<EventSystem>();
            Check("CHECK 1 — BootScene tiene InputSystemUIInputModule",
                es != null && es.GetComponent<InputSystemUIInputModule>() != null);

            var loginPanel = GameObject.Find("LoginPanel");
            int loginBtnsWithText = 0;
            if (loginPanel != null)
            {
                var btns = loginPanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in btns)
                {
                    var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl != null && !string.IsNullOrEmpty(lbl.text))
                        loginBtnsWithText++;
                }
            }
            Check("CHECK 2 — LoginPanel tiene >= 3 botones con texto no vacío",
                loginPanel != null && loginBtnsWithText >= 3);

            var btnGuest = GameObject.Find("BtnGuest");
            Check("CHECK 3 — BtnGuest tiene al menos 1 listener OnClick",
                btnGuest != null &&
                btnGuest.GetComponent<Button>() != null &&
                btnGuest.GetComponent<Button>().onClick.GetPersistentEventCount() >= 1);

            var controller = Object.FindFirstObjectByType<BootSceneController>();
            Check("CHECK 4 — BootSceneController está en la escena",
                controller != null);
        }

        // ── MainMenuScene ─────────────────────────────────────────────────────
        if (!System.IO.File.Exists(MAIN_PATH))
        {
            Debug.LogError("[S19fix] MainMenuScene.unity no encontrada — ejecuta primero Tools → 2. Setup MainMenuScene");
        }
        else
        {
            EditorSceneManager.OpenScene(MAIN_PATH, OpenSceneMode.Single);

            var content = GameObject.Find("ContentEdificios");
            int edificios = 0;
            if (content != null)
            {
                foreach (Transform child in content.transform)
                    if (child.GetComponent<Button>() != null)
                        edificios++;
            }
            Check("CHECK 5 — MainMenuScene tiene >= 9 edificios con Button",
                content != null && edificios >= 9);
        }

        // ── CampaignScene ─────────────────────────────────────────────────────
        if (!System.IO.File.Exists(CAMP_PATH))
        {
            Debug.LogError("[S19fix] CampaignScene.unity no encontrada — ejecuta primero Tools → 6. Setup CampaignScene");
        }
        else
        {
            EditorSceneManager.OpenScene(CAMP_PATH, OpenSceneMode.Single);

            var controller = Object.FindFirstObjectByType<CampaignSceneController>();
            Check("CHECK 6 — CampaignSceneController está en la escena",
                controller != null);
        }

        Debug.Log($"[S19fix] Resultado: {passed} PASS / {failed} FAIL");

        if (failed == 0)
            Debug.Log("[S19fix] Todas las comprobaciones pasaron. Puedes borrar S19fix_Test.cs antes del commit.");
        else
            Debug.LogError("[S19fix] Hay fallos. Ejecuta los Setup correspondientes y repite el test.");
    }
}
#endif
