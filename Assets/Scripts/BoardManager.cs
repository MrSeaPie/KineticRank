using UnityEngine;

public class BoardManager : MonoBehaviour
{
    // ───────────────── Grid settings ─────────────────
    [Header("Grid settings")]
    public int columns = 7;
    public int rows    = 6;

    // ───────────────── Prefabs & parent ──────────────
    [Header("Prefabs")]
    public GameObject boardTilePrefab;
    public GameObject redTokenPrefab;
    public GameObject blueTokenPrefab;
    public Transform  boardParent;

    // ───────────────── Origin ─────────────────────────
    // Bottom-left world-space corner of the board.
    // Change these numbers once if you want to move the whole board.
    [Header("Origin (bottom-left)")]
    public Vector2 origin = new Vector2(-3.5f, -2.5f);

    // Keeps track of which token sits in each slot
    private GameObject[,] tokenGrid;

    // ───────────────── Unity life-cycle ───────────────
    void Start()
    {
        tokenGrid = new GameObject[columns, rows];
        GenerateBoard();
    }

    // ───────────────── Board generation ───────────────
    void GenerateBoard()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // centre-of-square position
                Vector2 pos = origin + new Vector2(x + 0.5f, y + 0.5f);

                GameObject tile = Instantiate(boardTilePrefab, pos, Quaternion.identity);
                if (boardParent != null) tile.transform.SetParent(boardParent, false);
            }
        }
    }

    // ───────────────── Public API ─────────────────────
    /// <summary>
    /// Called by PlayerController. Spawns a token in the column;
    /// returns true if anything was placed (false if the column is full).
    /// </summary>
    public bool PlaceToken(int column, GameObject tokenPrefab)
    {
        // 1. Search from bottom row up for an empty slot
        for (int y = 0; y < rows; y++)
        {
            if (tokenGrid[column, y] == null)
            {
                // 2. Spawn the token
                Vector2 spawnPos  = origin + new Vector2(column + 0.5f, y + 0.5f);   // NEW
                GameObject token  = Instantiate(tokenPrefab, spawnPos, Quaternion.identity);
                if (boardParent != null) token.transform.SetParent(boardParent, false);

                // 2b. Tell the token to animate its fall
                Token tokenScript = token.GetComponent<Token>();
                Vector3 targetPos = origin + new Vector2(column + 0.5f, y + 0.5f);   // NEW
                tokenScript.AnimateFall(targetPos);

                // 3. Remember it
                tokenGrid[column, y] = token;

                // 4. Notify GameManager
                bool isRedToken = tokenPrefab == redTokenPrefab;
                GameManager.Instance?.OnTokenPlaced(column, y, isRedToken);

                return true;     // SUCCESS – only one return inside the loop
            }
        }

        Debug.Log("Column full");
        return false;            // FAILURE – couldn't place
    }

    // ───────────── Helpers used by GameManager ─────────
    public bool WithinBounds(int x, int y)
    {
        return 0 <= x && x < columns && 0 <= y && y < rows;
    }

    public bool IsTokenColor(int x, int y, bool isRed)
    {
        GameObject t = tokenGrid[x, y];
        return t != null && t.CompareTag(isRed ? "TokenRed" : "TokenBlue");
    }
}
