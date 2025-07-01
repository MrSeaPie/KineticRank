using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Scene references")]
    public BoardManager board;          // drag BoardManager here
    [Header("Token prefabs")]
    public GameObject redTokenPrefab;
    public GameObject blueTokenPrefab;
    [Header("Layer masks")]
    public LayerMask boardLayer;        // set to “BoardTile”

    /* state */
    private bool currentPlayerIsRed = true;

    void Update ()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // 1. Convert mouse → world
        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 2. Try a ray-cast (hits any BoardTile)
        RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero, 0f, boardLayer);

        // 3. Case A – we DID hit a tile  ↓
        if (hit.collider != null)
        {
            AddTokenAtWorldX(hit.point.x);
            return;
        }

        // 4. Case B – we DIDN’T hit a tile, but maybe we clicked just above it  ↓
        //    Use the mouse-x anyway, if it is horizontally over the board.
        float boardLeft  = board.origin.x;
        float boardRight = board.origin.x + board.columns;
        if (world.x > boardLeft && world.x < boardRight)
        {
            AddTokenAtWorldX(world.x);
        }
    }

    // ───────────────── helpers ─────────────────
    void AddTokenAtWorldX(float worldX)
    {
        // convert world-x → 0…columns-1
        int column = Mathf.Clamp(Mathf.FloorToInt(worldX - board.origin.x), 0, board.columns - 1);

        bool ok = board.PlaceToken(column,
                                   currentPlayerIsRed ? redTokenPrefab : blueTokenPrefab);
        if (ok) currentPlayerIsRed = !currentPlayerIsRed;
    }
}
