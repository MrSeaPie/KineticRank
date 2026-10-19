using UnityEngine;
using UnityEngine.SceneManagement;

// Creates the Solo bot driver in any scene that has a BoardManager while Solo mode is on.
// No scene or prefab carries a SoloBotDriver, so without this the bot never takes its turn.
public static class SoloBotBootstrap
{
    const string DriverName = "__SoloBot";

    static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!hooked)
        {
            SceneManager.sceneLoaded += (scene, mode) => Ensure();
            hooked = true;
        }
        Ensure();
    }

    // Returns the scene's driver, creating it if needed; null when Solo is off or there is no board.
    // Safe to call repeatedly: an existing driver is reused (and re-wired) rather than duplicated.
    public static SoloBotDriver Ensure()
    {
        if (!SoloSettings.Enabled) return null;

        var board = Object.FindObjectOfType<BoardManager>();
        if (board == null) return null;
        var player = Object.FindObjectOfType<PlayerController>();

        var driver = Object.FindObjectOfType<SoloBotDriver>();
        if (driver == null)
            driver = new GameObject(DriverName).AddComponent<SoloBotDriver>();

        if (driver.board == null) driver.board = board;
        if (driver.player == null) driver.player = player;
        return driver;
    }
}
