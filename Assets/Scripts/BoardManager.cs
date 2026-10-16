using System.Collections;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Size")]
    public int columns = 7;
    public int rows    = 6;

    [Header("Prefabs")]
    public GameObject boardTilePrefab;
    public GameObject redTokenPrefab;
    public GameObject blueTokenPrefab;

    [Header("Parent (optional)")]
    public Transform boardParent;

    // Runtime state
    private GameObject[,] tokenGrid;
    private const float tileStep = 1f;
    private const float originX  = 0.5f;

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

    // ───────────────── helpers ─────────────────
    public static Vector2 GridToWorld(int x, int y) => new(originX + x * tileStep, y + 0.5f);
    public static int WorldToColumn(float worldX)   => Mathf.RoundToInt((worldX - originX) / tileStep);

    // ───────────────── API (called by PlayerController) ─────────────────

    // Normal piece: classic drop
    public bool PlaceNormalToken(int column, GameObject normalPrefab, bool isRed)
    {
        if (!PrepareForLanding(column, out int y)) return false;

        var tok = SpawnToken(normalPrefab, column, y);
        GameManager.Instance?.OnTokenPlaced(column, y, isRed);
        return true;
    }

    // Queen: lands, flips column, then becomes a NORMAL piece in that same cell
    public bool PlaceQueen(int column, GameObject queenVisualPrefab, GameObject finalNormalPrefab, bool isRed)
    {
        if (!PrepareForLanding(column, out int y)) return false;

        var queenTok = SpawnToken(queenVisualPrefab, column, y);
        StartCoroutine(DoQueenAfterLand(column, y, queenTok, finalNormalPrefab, isRed));
        return true;
    }

    // Cleaner: lands, then the entire column disappears (including the cleaner)
    public bool PlaceCleaner(int column, GameObject cleanerPrefab)
    {
        if (!PrepareForLanding(column, out int y)) return false;

        var cleanerTok = SpawnToken(cleanerPrefab, column, y);
        StartCoroutine(DoCleanerAfterLand(column, y, cleanerTok));
        return true;
    }

    // ───────────────── effects ─────────────────

    // NOTE: No win-checks here; we only notify once per actual move.
    public void FlipColumn(int column)
    {
        if (column < 0 || column >= columns) return;

        for (int y = 0; y < rows; ++y)
        {
            var oldTok = tokenGrid[column, y];
            if (!oldTok) continue;

            bool wasRed = oldTok.CompareTag("TokenRed");
            Vector3 pos = oldTok.transform.position;
            Destroy(oldTok);

            var newPrefab = wasRed ? blueTokenPrefab : redTokenPrefab;
            var newTok    = Instantiate(newPrefab, pos, Quaternion.identity);
            if (boardParent) newTok.transform.SetParent(boardParent, false);
            tokenGrid[column, y] = newTok;

            // IMPORTANT: do NOT call GameManager.OnTokenPlaced() here.
        }
    }

    public void ClearColumn(int column)
    {
        if (column < 0 || column >= columns) return;

        for (int y = 0; y < rows; ++y)
        {
            if (tokenGrid[column, y])
            {
                Destroy(tokenGrid[column, y]);
                tokenGrid[column, y] = null;
            }
        }
    }

    // ───────────────── internals ─────────────────

    // Push-down rule: if top is full, pop bottom and shift DOWN; returns the landing Y index.
    private bool PrepareForLanding(int column, out int landingY)
    {
        landingY = -1;
        if (column < 0 || column >= columns) return false;

        if (tokenGrid[column, rows - 1] != null)
        {
            // pop bottom
            if (tokenGrid[column, 0]) Destroy(tokenGrid[column, 0]);

            // shift everything DOWN
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
            tokenGrid[column, rows - 1] = null; // free the top cell
        }

        // first empty slot
        for (int y = 0; y < rows; y++)
        {
            if (tokenGrid[column, y] == null)
            {
                landingY = y;
                return true;
            }
        }
        return false;
    }

    private GameObject SpawnToken(GameObject prefab, int column, int y)
    {
        Vector2 spawn  = GridToWorld(column, rows + 2);
        Vector2 target = GridToWorld(column, y);

        var tok = Instantiate(prefab, spawn, Quaternion.identity);
        if (boardParent) tok.transform.SetParent(boardParent, false);

        // Requires Token.cs on the prefab
        tok.GetComponent<Token>().AnimateFall(target);

        tokenGrid[column, y] = tok;
        return tok;
    }

    // Wait for Token.HasArrived flag (set by Token.cs) — no timing guesses
    private IEnumerator WaitUntilLanded(GameObject tok)
    {
        if (!tok) yield break;
        var t = tok.GetComponent<Token>();
        if (!t) { yield return null; yield break; } // safety if component missing
        while (tok && !t.HasArrived) yield return null;
    }

    private IEnumerator DoQueenAfterLand(int column, int y, GameObject queenTok, GameObject finalNormalPrefab, bool isRed)
    {
        // Ensure the queen visual actually sits in its cell first
        yield return WaitUntilLanded(queenTok);

        // Flip the whole column (this spawns a flipped token at [column,y])
        FlipColumn(column);

        // Remove whatever FlipColumn just put in this exact cell,
        // so we don't end up with two tokens layered here.
        if (tokenGrid[column, y] != null)
        {
            Destroy(tokenGrid[column, y]);
            tokenGrid[column, y] = null;
        }

        // Remove the queen visual (if still present)
        if (queenTok) Destroy(queenTok);

        // Now place the final normal piece (correct colour) that should remain
        Vector2 cellPos = GridToWorld(column, y);
        var norm = Instantiate(finalNormalPrefab, cellPos, Quaternion.identity);
        if (boardParent) norm.transform.SetParent(boardParent, false);
        tokenGrid[column, y] = norm;

        // One single win-check for the actual move outcome
        GameManager.Instance?.OnTokenPlaced(column, y, isRed);
    }

    private IEnumerator DoCleanerAfterLand(int column, int y, GameObject cleanerTok)
    {
        // Wait until the cleaner actually reaches the landing cell
        yield return WaitUntilLanded(cleanerTok);

        // Wipe the whole column (including the cleaner itself)
        ClearColumn(column);

        if (cleanerTok) Destroy(cleanerTok); // extra safety
    }

    // Utilities (used by win checks, etc.)
    public bool InBounds(int x, int y) => 0 <= x && x < columns && 0 <= y && y < rows;

    public bool IsToken(int x, int y, bool isRed)
    {
        var t = tokenGrid[x, y];
        return t && t.CompareTag(isRed ? "TokenRed" : "TokenBlue");
    }
}
