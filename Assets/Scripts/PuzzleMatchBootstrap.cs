using UnityEngine;
using UnityEngine.SceneManagement;

// Attaches PuzzleMatchDriver to any scene with a board while a puzzle is active; no scene edits needed
static class PuzzleMatchBootstrap
{
    const string DriverName = "PuzzleMatchDriver";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    static void Ensure()
    {
        if (!PuzzleMatchDriver.HasPuzzle) return;
        if (Object.FindObjectOfType<BoardManager>() == null) return;
        if (Object.FindObjectOfType<PuzzleMatchDriver>() != null) return;
        new GameObject(DriverName).AddComponent<PuzzleMatchDriver>();
    }
}
