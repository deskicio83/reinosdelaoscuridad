/*
============================================================
SplashScreen.cs — Pantalla de presentación
------------------------------------------------------------
PROPÓSITO
- Mostrar logo/animación breve y transicionar a Boot/MainMenu.

MÉTODOS (COMPLETA AQUÍ)
- Start(): lanza temporizador; SceneLoader.Load("MainMenu").
============================================================
*/

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float holdTime = 3f;
    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private string nextSceneName = "BootScene";
    [SerializeField] private Image splashImage;

    private CanvasGroup logoGroup;

    void Start()
    {
        if (SceneFader.Instance == null)
        {
            var go = new GameObject("SceneFader", typeof(SceneFader));
            DontDestroyOnLoad(go);
        }

        if (splashImage != null)
        {
            logoGroup = splashImage.GetComponent<CanvasGroup>() ?? splashImage.gameObject.AddComponent<CanvasGroup>();
            logoGroup.alpha = 0f;
        }

        SceneFader.Instance.FadeOut(() =>
        {
            Sequence seq = DOTween.Sequence().SetUpdate(true);
            if (logoGroup != null)
            {
                seq.Append(logoGroup.DOFade(1f, fadeInTime));
                seq.AppendInterval(holdTime);
                seq.Append(logoGroup.DOFade(0f, fadeOutTime));
            }
            else
            {
                seq.AppendInterval(holdTime);
            }

            seq.OnComplete(() => SceneFader.Instance.FadeToScene(nextSceneName));
        });
    }
}
