using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using KineticRank.Logic;

[DefaultExecutionOrder(-50)]
public class MoveClock : MonoBehaviour
{
    public static MoveClock Instance { get; private set; }

    [Header("Clock Settings")]
    [SerializeField] private float _turnDuration = 30f;
    public float turnDuration
    {
        get => _turnDuration;
        set
        {
            _turnDuration = value;
            State = new MoveClockState(_turnDuration, _warningThreshold);
        }
    }

    [SerializeField] private float _warningThreshold = 10f;
    public float warningThreshold
    {
        get => _warningThreshold;
        set
        {
            _warningThreshold = value;
            State = new MoveClockState(_turnDuration, _warningThreshold);
        }
    }

    [Header("UI Timer Bars")]
    public Image redTimerBar;
    public Image blueTimerBar;

    public MoveClockState State { get; private set; }
    public bool ActiveIsRed { get; private set; } = true;
    public bool MatchOver { get; private set; } = false;
    public bool SettledTimeout { get; private set; } = false;

    public static bool ForceRankedForTesting { get; set; } = false;

    PlayerController _cachedPlayerController;

    PlayerController GetPlayerController()
    {
        if (_cachedPlayerController == null)
            _cachedPlayerController = FindObjectOfType<PlayerController>();
        return _cachedPlayerController;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInstall()
    {
        if (UnityEngine.Object.FindObjectOfType<GameManager>() != null && Instance == null)
        {
            var go = new GameObject("MoveClock");
            go.AddComponent<MoveClock>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (State == null)
            State = new MoveClockState(_turnDuration, _warningThreshold);

        EnsureTimerBars();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static bool IsRankedMatch
    {
        get
        {
            if (ForceRankedForTesting) return true;

            var puzzleType = Type.GetType("PuzzleSettings");
            if (puzzleType != null)
            {
                var prop = puzzleType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
                if (prop != null && (bool)prop.GetValue(null)) return false;
            }

            var tutType = Type.GetType("TutorialSettings");
            if (tutType != null)
            {
                var prop = tutType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
                if (prop != null && (bool)prop.GetValue(null)) return false;
            }

            var tradType = Type.GetType("TraditionalSettings");
            if (tradType != null)
            {
                var prop = tradType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
                if (prop != null && (bool)prop.GetValue(null)) return false;
            }

            var soloType = Type.GetType("SoloSettings");
            if (soloType != null)
            {
                var isRankedProp = soloType.GetProperty("IsRanked", BindingFlags.Public | BindingFlags.Static);
                if (isRankedProp != null && (bool)isRankedProp.GetValue(null)) return true;
            }

            var rankedType = Type.GetType("RankedSettings");
            if (rankedType != null)
            {
                var prop = rankedType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
                if (prop != null && (bool)prop.GetValue(null)) return true;
            }

            return false;
        }
    }

    public void EnsureTimerBars()
    {
        if (redTimerBar == null || blueTimerBar == null)
        {
            var images = FindObjectsOfType<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name == "RedMoveClockBar" || img.gameObject.name == "RedTimerBar") redTimerBar = img;
                if (img.gameObject.name == "BlueMoveClockBar" || img.gameObject.name == "BlueTimerBar") blueTimerBar = img;
            }
        }

        if (redTimerBar == null || blueTimerBar == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                if (redTimerBar == null)
                {
                    var redGo = CreateBarObject(canvas.transform, "RedMoveClockBar", new Vector2(-220f, -180f), new Color(0.85f, 0.25f, 0.25f));
                    redTimerBar = redGo.GetComponent<Image>();
                }
                if (blueTimerBar == null)
                {
                    var blueGo = CreateBarObject(canvas.transform, "BlueMoveClockBar", new Vector2(220f, -180f), new Color(0.25f, 0.45f, 0.85f));
                    blueTimerBar = blueGo.GetComponent<Image>();
                }
            }
        }
    }

    static GameObject CreateBarObject(Transform parent, string name, Vector2 anchoredPos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 14f);
        rt.anchoredPosition = anchoredPos;
        var img = go.GetComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.color = color;
        return go;
    }

    public void StartTurn(bool isRed)
    {
        if (!IsRankedMatch) return;
        ActiveIsRed = isRed;
        SettledTimeout = false;
        if (State == null) State = new MoveClockState(_turnDuration, _warningThreshold);
        State.StartTurn(isRed);
        UpdateVisuals();
    }

    public void StopClock()
    {
        MatchOver = true;
        UpdateVisuals();
    }

    void Update()
    {
        if (MatchOver || SettledTimeout || !IsRankedMatch) return;

        if (HowToPlayOverlay.Instance != null && HowToPlayOverlay.Instance.IsOpen)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsResolvingMove)
            return;

        var pc = GetPlayerController();
        if (pc != null && pc.IsRedTurn != ActiveIsRed)
        {
            StartTurn(pc.IsRedTurn);
        }

        if (SoloSettings.Enabled && ActiveIsRed == SoloSettings.BotPlaysRed)
            return;

        if (State == null) State = new MoveClockState(_turnDuration, _warningThreshold);
        State.Tick(Time.deltaTime);
        UpdateVisuals();

        if (State.IsExpired && !SettledTimeout)
        {
            SettledTimeout = true;
            MatchOver = true;
            TriggerTimeoutForfeit(ActiveIsRed);
        }
    }

    public void TriggerTimeoutForfeit(bool forfeiterIsRed)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TimeoutForfeit(forfeiterIsRed);
        }
    }

    public void UpdateVisuals()
    {
        if (State == null) return;
        float fraction = _turnDuration > 0f ? Mathf.Clamp01(State.RemainingSeconds / _turnDuration) : 0f;
        bool isWarning = State.IsWarning;

        if (!IsRankedMatch)
        {
            if (redTimerBar != null) redTimerBar.gameObject.SetActive(false);
            if (blueTimerBar != null) blueTimerBar.gameObject.SetActive(false);
            return;
        }

        if (redTimerBar != null)
        {
            redTimerBar.gameObject.SetActive(true);
            if (ActiveIsRed)
            {
                redTimerBar.fillAmount = fraction;
                redTimerBar.color = isWarning ? Color.red : new Color(0.85f, 0.25f, 0.25f);
            }
            else
            {
                redTimerBar.fillAmount = 0f;
            }
        }

        if (blueTimerBar != null)
        {
            blueTimerBar.gameObject.SetActive(true);
            if (!ActiveIsRed)
            {
                blueTimerBar.fillAmount = fraction;
                blueTimerBar.color = isWarning ? Color.red : new Color(0.25f, 0.45f, 0.85f);
            }
            else
            {
                blueTimerBar.fillAmount = 0f;
            }
        }
    }
}
