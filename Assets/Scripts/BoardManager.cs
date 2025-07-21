using UnityEngine;

public class BoardManager : MonoBehaviour
{
    /* ─── grid size ─── */
    public int columns = 7;
    public int rows    = 6;

    /* ─── prefabs ─── */
    public GameObject boardTilePrefab;
    public GameObject redTokenPrefab;
    public GameObject blueTokenPrefab;
    public Transform  boardParent;        // holds tiles + tokens

    /* ─── runtime state ─── */
    GameObject[,] tokenGrid;              // null = empty
    public const float tileStep = 1f;     // distance (world units) between columns
    public const float originX  = 0.5f;   // x-coord of the centre of column 0

    void Start()
    {
        tokenGrid = new GameObject[columns, rows];
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows;    y++)
        {
            Vector2 pos = GridToWorld(x, y);
            var tile = Instantiate(boardTilePrefab, pos, Quaternion.identity);
            if (boardParent) tile.transform.SetParent(boardParent, false);
        }
    }

    /* convert grid → world & vice-versa */
    public static Vector2 GridToWorld(int x, int y) => new(originX + x * tileStep, y + 0.5f);
    public static int WorldToColumn(float worldX)   => Mathf.RoundToInt((worldX - originX) / tileStep);

    /* ───────────────── PlaceToken ───────────────── */
    public bool PlaceToken(int column, GameObject prefab)
    {
        if (column < 0 || column >= columns) return false;

        /* full column? → pop bottom + shift down */
        if (tokenGrid[column, rows - 1] != null)
        {
            if (tokenGrid[column, 0]) Destroy(tokenGrid[column, 0]);
            for (int r = 1; r < rows; r++)
            {
                tokenGrid[column, r - 1] = tokenGrid[column, r];
                if (tokenGrid[column, r - 1])
                {
                    var p = tokenGrid[column, r - 1].transform.position;
                    p.y -= tileStep;
                    tokenGrid[column, r - 1].GetComponent<Token>().AnimateFall(p);
                }
            }
            tokenGrid[column, rows - 1] = null;   // top slot now free
        }

        /* find first empty slot */
        for (int y = 0; y < rows; y++)
        {
            if (tokenGrid[column, y] == null)
            {
                Vector2 spawnPos  = GridToWorld(column, rows + 2);   // start well above
                Vector2 targetPos = GridToWorld(column, y);

                var tok = Instantiate(prefab, spawnPos, Quaternion.identity);
                if (boardParent) tok.transform.SetParent(boardParent, false);
                tok.GetComponent<Token>().AnimateFall(targetPos);

                tokenGrid[column, y] = tok;
                GameManager.Instance.OnTokenPlaced(column, y, prefab == redTokenPrefab);
                return true;
            }
        }
        return false;            // ← should never happen
    }

    /* helpers for GameManager */
    public bool InBounds(int x, int y) => 0 <= x && x < columns && 0 <= y && y < rows;
    public bool IsToken(int x, int y, bool isRed)
    {
        var t = tokenGrid[x, y];
        return t && t.CompareTag(isRed ? "TokenRed" : "TokenBlue");
    }
}