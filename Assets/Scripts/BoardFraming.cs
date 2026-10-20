using UnityEngine;
using UnityEngine.SceneManagement;

// Presentation-only: zooms the Match camera so the board fills the space between the two token panels
// (PowerUpHud), with a row above it for the selected-token preview. Refitted every frame, so a window resize
// keeps it fitted. Board coordinates, rules and click mapping are unchanged (clicks still go through Camera.main).
public class BoardFraming : MonoBehaviour
{
    // World-unit margins around the board's wooden frame, and the preview row above it
    const float SideMargin = 0.3f;
    const float BottomMargin = 0.3f;
    const float PreviewSpace = 1.25f;
    // Reference pixels kept free above and below the board
    const float VerticalMargin = 24f;

    BoardManager board;

    // Self-attaches to any scene that has a BoardManager, so no scene edits are needed
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    static void Attach()
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;
        foreach (var existing in FindObjectsOfType<BoardFraming>())
            if (existing.gameObject.scene == board.gameObject.scene) return;

        var go = new GameObject("BoardFraming");
        SceneManager.MoveGameObjectToScene(go, board.gameObject.scene);
        go.AddComponent<BoardFraming>().board = board;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (board == null || cam == null || !cam.orthographic || Screen.width <= 0 || Screen.height <= 0) return;

        float left = BoardManager.GridToWorld(0, 0).x - 0.5f - SideMargin;
        float right = BoardManager.GridToWorld(board.columns - 1, 0).x + 0.5f + SideMargin;
        float bottom = -BottomMargin;
        float top = board.rows + PreviewSpace;

        // Same scale factor as a CanvasScaler at 1920 x 1080 with match 0.5
        float scale = Mathf.Sqrt(Screen.width / PowerUpHud.ReferenceWidth * (Screen.height / PowerUpHud.ReferenceHeight));
        float panels = 2f * (PowerUpHud.PanelWidth + PowerUpHud.EdgeMargin + PowerUpHud.BoardGap) * scale;
        float regionWidth = Mathf.Max(Screen.width * 0.4f, Screen.width - panels);
        float regionHeight = Mathf.Max(1f, Screen.height - 2f * VerticalMargin * scale);

        // One world unit is Screen.height / (2 * size) pixels, so each limit sets a minimum size
        float forHeight = (top - bottom) * 0.5f * Screen.height / regionHeight;
        float forWidth = (right - left) * 0.5f * Screen.height / regionWidth;
        cam.orthographicSize = Mathf.Max(forHeight, forWidth);

        var position = cam.transform.position;
        cam.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, position.z);
    }
}
