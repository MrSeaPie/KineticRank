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
            headingStyle.normal.textColor = new Color(0.953f, 0.910f, 0.824f);
            detailStyle.normal.textColor = headingStyle.normal.textColor;
            hintStyle.normal.textColor = new Color(0.886f, 0.678f, 0.447f);
        }

        // Screen.safeArea uses bottom-left coordinates; IMGUI uses top-left.
        Rect safe = Screen.safeArea;
        float width = Mathf.Min(620f, Mathf.Max(0f, safe.width - 20f));
        if (width < 1f) return;
        float scale = Mathf.Clamp(width / 620f, 0.5f, 1f);
        float height = 88f * scale;
        float left = safe.x + (safe.width - width) * 0.5f;
        float top = Screen.height - safe.y - 10f - height;
        Rect panel = new Rect(left, top, width, height);
        Color previousColor = GUI.color;
        Color previousBackground = GUI.backgroundColor;
        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0.149f, 0.082f, 0.043f, 0.96f);
        GUI.Box(panel, GUIContent.none);
        GUI.backgroundColor = previousBackground;

        headingStyle.fontSize = Mathf.RoundToInt(18f * scale);
        detailStyle.fontSize = Mathf.RoundToInt(14f * scale);
        hintStyle.fontSize = Mathf.RoundToInt(14f * scale);
        float x = left + 14f * scale;
        float y = top + 6f * scale;
        float innerWidth = width - 28f * scale;
        var manager = GameManager.Instance;
        string status = manager != null && manager.IsResolvingMove
            ? "Resolving move..."
            : $"{(player.IsRedTurn ? "RED" : "BLUE")} to move";
        GUI.Label(new Rect(x, y, innerWidth, 25f * scale),
            $"{status}   |   {player.CurrentPieceName}", headingStyle);
        y += 26f * scale;
        float gap = 12f * scale;
        float columnWidth = (innerWidth - gap) * 0.5f;
        GUI.Label(new Rect(x, y, columnWidth, 22f * scale), Uses(true), detailStyle);
        GUI.Label(new Rect(x + columnWidth + gap, y, columnWidth, 22f * scale), Uses(false), detailStyle);
        y += 23f * scale;
        GUI.Label(new Rect(x, y, innerWidth, 23f * scale),
            "Select: N Normal / Q Queen / C Cleaner", hintStyle);
        GUI.color = previousColor;
    }

    string Uses(bool red)
    {
        string queen = player.QueenAvailable(red) ? "ready" : "used";
        string cleaner = player.CleanerAvailable(red) ? "ready" : "used";
        return $"{(red ? "RED" : "BLUE")}   Queen: {queen} | Cleaner: {cleaner}";
    }
}
