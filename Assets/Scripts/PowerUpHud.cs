using UnityEngine;
using UnityEngine.SceneManagement;

// Shows whose turn it is, the selected piece and each side's remaining power-ups.
// Spawns itself in any scene that has a PlayerController, so no scene edit is needed.
public class PowerUpHud : MonoBehaviour
{
    PlayerController player;
    GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        // unsubscribe first so a disabled domain reload can't double-register
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

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            style.normal.textColor = Color.white;
        }

        string text =
            $"Turn: {(player.IsRedTurn ? "Red" : "Blue")}   Piece: {player.CurrentPieceName}   (N / Q / C)\n" +
            Uses(true) + "\n" +
            Uses(false);

        GUI.Label(new Rect(10, Screen.height - 90, 600, 80), text, style);
    }

    string Uses(bool red)
    {
        string queen   = player.QueenAvailable(red)   ? "ready" : "used";
        string cleaner = player.CleanerAvailable(red) ? "ready" : "used";
        return $"{(red ? "Red " : "Blue")}  Queen: {queen}   Cleaner: {cleaner}";
    }
}
