using UnityEngine;
using UnityEngine.SceneManagement;

// Display-only match information; keyboard and board input stay with PlayerController.
public class PowerUpHud : MonoBehaviour
{
    PlayerController player;
    GUIStyle headingStyle;
    GUIStyle detailStyle;
    GUIStyle hintStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc == null || FindObjectOfType<PowerUpHud>() != null) return;
        var go = new GameObject("PowerUpHud");
        go.AddComponent<PowerUpHud>().player = pc;
    }

    void OnGUI()
    {
        if (player == null || !player.enabled) return;
        if (headingStyle == null)
        {
            headingStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            detailStyle = new GUIStyle(GUI.skin.label);
            hintStyle = new GUIStyle(GUI.skin.label);
            headingStyle.normal.textColor = Color.white;
            detailStyle.normal.textColor = Color.white;
            hintStyle.normal.textColor = new Color(0.8f, 0.85f, 0.92f);
        }

        // Screen.safeArea uses bottom-left coordinates; IMGUI uses top-left.
        Rect safe = Screen.safeArea;
        float width = Mathf.Min(620f, Mathf.Max(0f, safe.width - 20f));
        if (width < 1f) return;
        float scale = Mathf.Clamp(width / 620f, 0.5f, 1f);
        float height = 124f * scale;
        float left = safe.x + (safe.width - width) * 0.5f;
        float top = Screen.height - safe.y - 10f - height;
        Rect panel = new Rect(left, top, width, height);
        Color previousColor = GUI.color;
        Color previousBackground = GUI.backgroundColor;
        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0.08f, 0.12f, 0.2f, 0.96f);
        GUI.Box(panel, GUIContent.none);
        GUI.backgroundColor = previousBackground;

        headingStyle.fontSize = Mathf.RoundToInt(20f * scale);
        detailStyle.fontSize = Mathf.RoundToInt(17f * scale);
        hintStyle.fontSize = Mathf.RoundToInt(15f * scale);
        float x = left + 14f * scale;
        float y = top + 8f * scale;
        float innerWidth = width - 28f * scale;
        var manager = GameManager.Instance;
        string status = manager != null && manager.IsResolvingMove
            ? "Resolving move..."
            : $"{(player.IsRedTurn ? "RED" : "BLUE")} to move";
        GUI.Label(new Rect(x, y, innerWidth, 28f * scale),
            $"{status}   |   {player.CurrentPieceName}", headingStyle);
        y += 30f * scale;
        GUI.Label(new Rect(x, y, innerWidth, 24f * scale), Uses(true), detailStyle);
        y += 24f * scale;
        GUI.Label(new Rect(x, y, innerWidth, 24f * scale), Uses(false), detailStyle);
        y += 25f * scale;
        GUI.Label(new Rect(x, y, innerWidth, 23f * scale),
            "Select: N Normal / Q Queen / C Cleaner", hintStyle);
        GUI.color = previousColor;
    }

    string Uses(bool red)
    {
        string queen = player.QueenAvailable(red) ? "ready" : "used";
        string cleaner = player.CleanerAvailable(red) ? "ready" : "used";
        return $"{(red ? "RED" : "BLUE")}   Queen: {queen}   |   Cleaner: {cleaner}";
    }
}
