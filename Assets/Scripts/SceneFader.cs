// File: Assets/Scripts/SceneFader.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    private static SceneFader _instance;
    private Canvas _canvas;
    private Image _overlay;
    private CanvasGroup _group;

    // Accessor that auto-creates the fader if missing.
    public static SceneFader Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("__SceneFader");
                _instance = go.AddComponent<SceneFader>();
                _instance.BuildOverlay();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void BuildOverlay()
    {
        // Canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = short.MaxValue; // draw on top of everything

        gameObject.AddComponent<GraphicRaycaster>(); // optional; can block clicks while fading

        // Fullscreen child with Image + CanvasGroup
        var overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(transform, false);

        _overlay = overlayGO.AddComponent<Image>();
        _overlay.color = Color.black; // black fade
        _group = overlayGO.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;

        // Stretch to full screen
        var rt = overlayGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Cross-fade to a scene and back (fade-out -> load -> fade-in)
    /// </summary>
    public static void LoadScene(string sceneName, float duration = 0.3f)
    {
        Instance.StartCoroutine(Instance.FadeRoutine(sceneName, duration));
    }

    IEnumerator FadeRoutine(string sceneName, float duration)
    {
        if (_group == null) BuildOverlay();

        // Fade out
        _group.blocksRaycasts = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration))
        {
            _group.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        _group.alpha = 1f;

        // Load scene
        yield return null; // one frame to ensure full-opaque
        SceneManager.LoadScene(sceneName);

        // Fade in
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration))
        {
            _group.alpha = Mathf.SmoothStep(1f, 0f, t);
            yield return null;
        }
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }
}
