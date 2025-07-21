using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Scene references")]
    public BoardManager  board;
    public GameObject    redTokenPrefab;
    public GameObject    blueTokenPrefab;

    bool isRedTurn = true;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int col = BoardManager.WorldToColumn(world.x);

        GameObject prefab = isRedTurn ? redTokenPrefab : blueTokenPrefab;
        board.PlaceToken(col, prefab);      // we don’t care about the bool any more
        isRedTurn = !isRedTurn;
    }

    public void SetTurn(bool nextIsRed) => isRedTurn = nextIsRed;
}
