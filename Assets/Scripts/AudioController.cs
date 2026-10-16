// File: Assets/Scripts/AudioController.cs
using UnityEngine;

/// <summary>
/// Lightweight, zero-setup audio helper:
/// - Auto-creates itself on first use and persists across scenes
/// - Looks for optional clips under Resources/Audio:
///     ui_click.wav / .mp3
///     win_red.wav
///     win_blue.wav
/// - If clips are missing, methods safely no-op.
/// </summary>
public class AudioController : MonoBehaviour
{
    private static AudioController _instance;
    public static AudioController Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("__AudioController");
                _instance = go.AddComponent<AudioController>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    AudioSource _sourceUI;
    AudioSource _sourceFX;

    AudioClip _click;
    AudioClip _winRed;
    AudioClip _winBlue;

    [Range(0f, 1f)] public float uiVolume = 0.7f;
    [Range(0f, 1f)] public float fxVolume = 0.9f;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _sourceUI = gameObject.AddComponent<AudioSource>();
        _sourceFX = gameObject.AddComponent<AudioSource>();

        // Try to load optional clips from Resources/Audio/
        _click   = Resources.Load<AudioClip>("Audio/ui_click");
        _winRed  = Resources.Load<AudioClip>("Audio/win_red");
        _winBlue = Resources.Load<AudioClip>("Audio/win_blue");
    }

    public void PlayClick()
    {
        if (_click == null) return;
        _sourceUI.PlayOneShot(_click, uiVolume);
    }

    public void PlayWin(bool red)
    {
        var clip = red ? _winRed : _winBlue;
        if (clip == null) return;
        _sourceFX.PlayOneShot(clip, fxVolume);
    }
}
