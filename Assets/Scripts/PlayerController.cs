using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public BoardManager board;

    [Header("Normal tokens")]
    public GameObject redTokenPrefab;
    public GameObject blueTokenPrefab;

    [Header("Power-ups (visual discs that drop)")]
    public GameObject redQueenPrefab;
    public GameObject blueQueenPrefab;
    public GameObject redCleanerPrefab;
    public GameObject blueCleanerPrefab;

    bool isRedTurn = true;

    bool redQueenUsed = false, redCleanerUsed = false;
    bool blueQueenUsed = false, blueCleanerUsed = false;

    enum PieceType { Normal, Queen, Cleaner }
    PieceType currentPiece = PieceType.Normal;

    // Read-only state for HUD display
    public bool IsRedTurn => isRedTurn;
    public string CurrentPieceName => currentPiece.ToString();
    public bool QueenAvailable(bool red)   => red ? !redQueenUsed   : !blueQueenUsed;
    public bool CleanerAvailable(bool red) => red ? !redCleanerUsed : !blueCleanerUsed;

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsInputLocked || GameManager.Instance.IsMatchOver))
            return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            currentPiece = PieceType.Normal;
            if (AudioController.Instance != null) AudioController.Instance.PlayTokenSelect();
        }
        if (Input.GetKeyDown(KeyCode.Q)) TrySelect(PieceType.Queen);
        if (Input.GetKeyDown(KeyCode.C)) TrySelect(PieceType.Cleaner);

        if (Input.GetMouseButtonDown(0))
        {
            if (board == null || Camera.main == null) return;

            var world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int col = BoardManager.WorldToColumn(world.x);

            bool placed = false;

            switch (currentPiece)
            {
                case PieceType.Normal:
                    placed = board.PlaceNormalToken(col,
                                isRedTurn ? redTokenPrefab : blueTokenPrefab,
                                isRedTurn);
                    break;

                case PieceType.Queen:
                    // one per player (selection already rejects a used queen; kept as a safety net)
                    if (!QueenAvailable(isRedTurn))
                    {
                        currentPiece = PieceType.Normal;
                        if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
                        return;
                    }

                    placed = board.PlaceQueen(col,
                                isRedTurn ? redQueenPrefab : blueQueenPrefab,    // visual disc that drops
                                isRedTurn ? redTokenPrefab : blueTokenPrefab,    // the normal piece that stays
                                isRedTurn);

                    if (placed)
                        if (isRedTurn) redQueenUsed = true; else blueQueenUsed = true;
                    break;

                case PieceType.Cleaner:
                    if (!CleanerAvailable(isRedTurn))
                    {
                        currentPiece = PieceType.Normal;
                        if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
                        return;
                    }

                    placed = board.PlaceCleaner(col,
                                isRedTurn ? redCleanerPrefab : blueCleanerPrefab);

                    if (placed)
                        if (isRedTurn) redCleanerUsed = true; else blueCleanerUsed = true;
                    break;
            }

            if (placed)
            {
                // consume turn for ANY move
                isRedTurn = !isRedTurn;
                // always go back to Normal after a drop
                currentPiece = PieceType.Normal;
            }
            else
            {
                // rejected move (full column, off-board click, etc.); drop/effect SFX live in BoardManager
                if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
            }
        }
    }

    // Only select a power-up the current player still has; otherwise stay on Normal
    void TrySelect(PieceType piece)
    {
        bool available = piece == PieceType.Queen ? QueenAvailable(isRedTurn) : CleanerAvailable(isRedTurn);
        if (!available)
        {
            Debug.Log($"{(isRedTurn ? "Red" : "Blue")} {piece.ToString().ToLower()} already used.");
            currentPiece = PieceType.Normal;
            if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
            return;
        }
        currentPiece = piece;
        if (AudioController.Instance != null) AudioController.Instance.PlayTokenSelect();
    }
}
