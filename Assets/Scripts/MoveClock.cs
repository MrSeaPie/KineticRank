using System;
using UnityEngine;
using UnityEngine.UI;
using KineticRank.Logic;

public class MoveClock : MonoBehaviour
{
    public static MoveClock Instance { get; private set; }

    [Header("UI References")]
    public Image redTimerBar;
    public Image blueTimerBar;

    [Header("Settings")]
    public float turnDuration = 30f;
    public float warningThreshold = 10f;

    public static bool IsRankedMatch { get; set; } = false;

    public MoveClockState State { get; private set; }
    public bool ActiveIsRed => activeIsRed;

    bool activeIsRed = true;
    bool isOverlayPaused = false;
    bool isBotPaused = false;
    bool matchOver = false;

    Color normalColor = new Color(0.2f, 0.7f, 0.2f, 1f);
    Color warningColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    void Awake()
    {
        Instance = this;
        State = new MoveClockState(turnDuration, warningThreshold);
    }

    void OnEnable()
    {
        PlayerController.MovePlaced += OnMovePlaced;
    }

    void OnDisable()
    {
        PlayerController.MovePlaced -= OnMovePlaced;
    }

    void Start()
    {
        if (PuzzleSettings.Enabled || !IsRankedMatch)
        {
            enabled = false;
            HideBars();
            return;
        }

        activeIsRed = RematchTracker.CurrentFirstMoverIsRed;
        State.StartTurn();
        UpdateVisuals();
    }

    void Update()
    {
        if (matchOver) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsMatchOver || GameManager.Instance.IsInputLocked))
            return;

        bool overlayOpen = HowToPlayOverlay.Instance != null && HowToPlayOverlay.Instance.gameObject.activeInHierarchy;
        if (overlayOpen != isOverlayPaused)
        {
            isOverlayPaused = overlayOpen;
            if (isOverlayPaused) State.Pause();
            else if (!isBotPaused) State.Resume();
        }

        PlayerController pc = FindObjectOfType<PlayerController>();
        bool botThinking = pc != null && !pc.IsHumanTurn;
        if (botThinking != isBotPaused)
        {
            isBotPaused = botThinking;
            if (isBotPaused) State.Pause();
            else if (!isOverlayPaused) State.Resume();
        }

        if (State.IsPaused) return;

        State.Tick(Time.deltaTime);
        UpdateVisuals();

        if (State.IsExpired)
        {
            matchOver = true;
            OnTimeout();
        }
    }

    void OnMovePlaced(int col, MoveKind kind, bool moverWasRed)
    {
        if (matchOver) return;

        activeIsRed = !moverWasRed;
        State.StartTurn();
        UpdateVisuals();
    }

    void OnTimeout()
    {
        bool redTimedOut = activeIsRed;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TimeoutForfeit(redTimedOut);
        }
    }

    void UpdateVisuals()
    {
        float frac = State.FractionRemaining;
        Color barColor = State.IsWarning ? warningColor : normalColor;

        if (redTimerBar != null)
        {
            bool show = activeIsRed && !matchOver;
            redTimerBar.gameObject.SetActive(show);
            if (show)
            {
                redTimerBar.fillAmount = frac;
                redTimerBar.color = barColor;
            }
        }

        if (blueTimerBar != null)
        {
            bool show = !activeIsRed && !matchOver;
            blueTimerBar.gameObject.SetActive(show);
            if (show)
            {
                blueTimerBar.fillAmount = frac;
                blueTimerBar.color = barColor;
            }
        }
    }

    void HideBars()
    {
        if (redTimerBar != null) redTimerBar.gameObject.SetActive(false);
        if (blueTimerBar != null) blueTimerBar.gameObject.SetActive(false);
    }
}
