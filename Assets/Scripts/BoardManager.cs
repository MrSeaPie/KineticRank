using System.Collections;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int columns = 7, rows = 6;
    public GameObject boardTilePrefab, redTokenPrefab, blueTokenPrefab;
    public Transform boardParent;

    private GameObject[,] tokenGrid;
    private const float tileStep = 1f, originX = 0.5f;
    private bool resolvingMove;

    void Start()
    {
        tokenGrid = new GameObject[columns, rows];
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                var tile = Instantiate(boardTilePrefab, GridToWorld(x, y), Quaternion.identity);
                if (boardParent) tile.transform.SetParent(boardParent, true);
            }
    }

    public static Vector2 GridToWorld(int x, int y) => new Vector2(originX + x * tileStep, y + 0.5f);
    public static int WorldToColumn(float worldX) => Mathf.RoundToInt((worldX - originX) / tileStep);

    public bool PlaceNormalToken(int column, GameObject normalPrefab, bool isRed)
    {
        return BeginPlacement(column, normalPrefab, null, 0);
    }

    public bool PlaceQueen(int column, GameObject queenVisualPrefab, GameObject finalNormalPrefab, bool isRed)
    {
        if (!finalNormalPrefab || !redTokenPrefab || !blueTokenPrefab) return false;
        if (!finalNormalPrefab.GetComponent<Token>() || !redTokenPrefab.GetComponent<Token>() ||
            !blueTokenPrefab.GetComponent<Token>()) return false;
        return BeginPlacement(column, queenVisualPrefab, finalNormalPrefab, 1);
    }

    public bool PlaceCleaner(int column, GameObject cleanerPrefab)
    {
        return BeginPlacement(column, cleanerPrefab, null, 2);
    }

    private bool BeginPlacement(int column, GameObject prefab, GameObject finalPrefab, int effect)
    {
        if (resolvingMove || tokenGrid == null || column < 0 || column >= columns || rows < 1)
            return false;
        if (!prefab || !prefab.GetComponent<Token>()) return false;
        var manager = GameManager.Instance;
        if (manager && !manager.TryBeginMove()) return false;

        resolvingMove = true;
        int landingY;
        GetLandingRow(column, out landingY);
        SpawnToken(prefab, column, landingY);
        StartCoroutine(ResolveMove(column, landingY, finalPrefab, effect, manager));
        return true;
    }

    private void GetLandingRow(int column, out int landingY)
    {
        if (tokenGrid[column, rows - 1])
        {
            if (tokenGrid[column, 0]) Destroy(tokenGrid[column, 0]);
            for (int r = 1; r < rows; r++)
            {
                var token = tokenGrid[column, r];
                tokenGrid[column, r - 1] = token;
                if (token) AnimateOrSnap(token, GridToWorld(column, r - 1));
            }
            tokenGrid[column, rows - 1] = null;
            if (AudioController.Instance != null) AudioController.Instance.PlayColumnShift();
        }

        landingY = 0;
        while (landingY < rows - 1 && tokenGrid[column, landingY]) landingY++;
    }

    private GameObject SpawnToken(GameObject prefab, int column, int y)
    {
        var token = Instantiate(prefab, GridToWorld(column, rows + 2), Quaternion.identity);
        if (boardParent) token.transform.SetParent(boardParent, true);
        tokenGrid[column, y] = token;
        AnimateOrSnap(token, GridToWorld(column, y));
        return token;
    }

    private void AnimateOrSnap(GameObject token, Vector2 target)
    {
        var animation = token.GetComponent<Token>();
        if (animation) animation.AnimateFall(target);
        else token.transform.position = target;
    }

    private IEnumerator WaitForColumn(int column)
    {
        for (int y = 0; y < rows; y++)
        {
            var token = tokenGrid[column, y];
            if (!token) continue;
            var animation = token.GetComponent<Token>();
            while (token && animation && !animation.HasArrived) yield return null;
        }
    }

    private IEnumerator ResolveMove(int column, int y, GameObject finalPrefab, int effect, GameManager manager)
    {
        yield return WaitForColumn(column);
        if (AudioController.Instance != null) AudioController.Instance.PlayTokenDrop();
        if (effect == 1)
        {
            FlipColumn(column);
            if (tokenGrid[column, y]) Destroy(tokenGrid[column, y]);
            var normal = Instantiate(finalPrefab, GridToWorld(column, y), Quaternion.identity);
            if (boardParent) normal.transform.SetParent(boardParent, true);
            tokenGrid[column, y] = normal;
            normal.GetComponent<Token>().SettleAt(GridToWorld(column, y));
        }
        else if (effect == 2)
        {
            ClearColumn(column);
        }

        resolvingMove = false;
        if (manager) manager.CompleteMove();
    }

    public void FlipColumn(int column)
    {
        if (tokenGrid == null || column < 0 || column >= columns) return;
        if (!redTokenPrefab || !blueTokenPrefab) return;
        if (AudioController.Instance != null) AudioController.Instance.PlayQueenFlip();
        for (int y = 0; y < rows; y++)
        {
            var old = tokenGrid[column, y];
            if (!old) continue;
            var prefab = old.CompareTag("TokenRed") ? blueTokenPrefab : redTokenPrefab;
            var replacement = Instantiate(prefab, GridToWorld(column, y), Quaternion.identity);
            if (boardParent) replacement.transform.SetParent(boardParent, true);
            tokenGrid[column, y] = replacement;
            var animation = replacement.GetComponent<Token>();
            if (animation) animation.SettleAt(GridToWorld(column, y));
            Destroy(old);
        }
    }

    public void ClearColumn(int column)
    {
        if (tokenGrid == null || column < 0 || column >= columns) return;
        if (AudioController.Instance != null) AudioController.Instance.PlayCleanerClear();
        for (int y = 0; y < rows; y++)
        {
            if (tokenGrid[column, y]) Destroy(tokenGrid[column, y]);
            tokenGrid[column, y] = null;
        }
    }

    public bool InBounds(int x, int y) => 0 <= x && x < columns && 0 <= y && y < rows;

    public bool IsToken(int x, int y, bool isRed)
    {
        if (tokenGrid == null || !InBounds(x, y)) return false;
        var token = tokenGrid[x, y];
        return token && token.CompareTag(isRed ? "TokenRed" : "TokenBlue");
    }
}
