// File: Assets/Scripts/AudioController.cs
using UnityEngine;

/// <summary>
/// Lightweight, zero-setup audio helper:
/// - Auto-creates itself on first use and persists across scenes
/// - Looks for optional clips under Resources/Audio:
///     ui_click.wav / .mp3
///     win_red.wav
///     win_blue.wav
///     token_drop.wav
///     queen_flip.wav
///     cleaner_clear.wav
///     column_shift.wav
///     invalid_move.wav
///     token_select.wav
///     match_draw.wav
///     column_hover.wav
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
    AudioClip _tokenDrop;
    AudioClip _queenFlip;
    AudioClip _cleanerClear;
    AudioClip _columnShift;
    AudioClip _invalidMove;
    AudioClip _tokenSelect;
    AudioClip _matchDraw;
    AudioClip _columnHover;

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
        _click        = Resources.Load<AudioClip>("Audio/ui_click");
        _winRed       = Resources.Load<AudioClip>("Audio/win_red");
        _winBlue      = Resources.Load<AudioClip>("Audio/win_blue");
        _tokenDrop    = Resources.Load<AudioClip>("Audio/token_drop");
        _queenFlip    = Resources.Load<AudioClip>("Audio/queen_flip");
        _cleanerClear = Resources.Load<AudioClip>("Audio/cleaner_clear");
        _columnShift  = Resources.Load<AudioClip>("Audio/column_shift");
        _invalidMove  = Resources.Load<AudioClip>("Audio/invalid_move");
        _tokenSelect  = Resources.Load<AudioClip>("Audio/token_select");
        _matchDraw    = Resources.Load<AudioClip>("Audio/match_draw");
        _columnHover  = Resources.Load<AudioClip>("Audio/column_hover");
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

    public void PlayDraw()
    {
        if (_matchDraw == null) return;
        _sourceFX.PlayOneShot(_matchDraw, fxVolume);
    }

    public void PlayTokenDrop()
    {
        if (_tokenDrop == null) return;
        _sourceFX.PlayOneShot(_tokenDrop, fxVolume);
    }

    public void PlayQueenFlip()
    {
        if (_queenFlip == null) return;
        _sourceFX.PlayOneShot(_queenFlip, fxVolume);
    }

    public void PlayCleanerClear()
    {
        if (_cleanerClear == null) return;
        _sourceFX.PlayOneShot(_cleanerClear, fxVolume);
    }

    public void PlayColumnShift()
    {
        if (_columnShift == null) return;
        _sourceFX.PlayOneShot(_columnShift, fxVolume);
    }

    public void PlayInvalidMove()
    {
        if (_invalidMove == null) return;
        _sourceUI.PlayOneShot(_invalidMove, uiVolume);
    }

    public void PlayTokenSelect()
    {
        if (_tokenSelect == null) return;
        _sourceUI.PlayOneShot(_tokenSelect, uiVolume);
    }

    public void PlayColumnHover()
    {
        if (_columnHover == null) return;
        _sourceUI.PlayOneShot(_columnHover, uiVolume);
    }
}
