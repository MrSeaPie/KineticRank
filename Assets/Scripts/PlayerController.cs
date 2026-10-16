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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N)) currentPiece = PieceType.Normal;
        if (Input.GetKeyDown(KeyCode.Q)) currentPiece = PieceType.Queen;
        if (Input.GetKeyDown(KeyCode.C)) currentPiece = PieceType.Cleaner;

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
                    // one per player
                    if (isRedTurn && redQueenUsed)   { Debug.Log("Red queen already used.");  return; }
                    if (!isRedTurn && blueQueenUsed) { Debug.Log("Blue queen already used."); return; }

                    placed = board.PlaceQueen(col,
                                isRedTurn ? redQueenPrefab : blueQueenPrefab,    // visual disc that drops
                                isRedTurn ? redTokenPrefab : blueTokenPrefab,    // the normal piece that stays
                                isRedTurn);

                    if (placed)
                        if (isRedTurn) redQueenUsed = true; else blueQueenUsed = true;
                    break;

                case PieceType.Cleaner:
                    if (isRedTurn && redCleanerUsed)   { Debug.Log("Red cleaner already used.");  return; }
                    if (!isRedTurn && blueCleanerUsed) { Debug.Log("Blue cleaner already used."); return; }

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
        }
    }
}
