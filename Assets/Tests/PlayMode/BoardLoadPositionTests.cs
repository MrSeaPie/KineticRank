using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BoardLoadPositionTests
{
    const int Columns = 7, Rows = 6;
    GameObject root;
    Component board;
    Type boardType;
    Type tokenType;
    Type snapshotType;
    GameObject red;
    GameObject blue;
    GameObject[,] grid;

    // Rows are listed top-down: 'R' red, 'B' blue, '.' empty.
    // Layouts keep equal red and blue counts so red to move is a reachable position.
    static readonly string[] MidGame =
    {
        ".......",
        ".......",
        "..R....",
        "..B....",
        ".RBRB..",
        "BRRBRB.",
    };

    static readonly string[] FullColumn =
    {
        "......R",
        "......B",
        "......R",
        "......B",
        "......R",
        "......B",
    };

    static readonly string[] Empty =
    {
        ".......",
        ".......",
        ".......",
        ".......",
        ".......",
        ".......",
    };

    static Type RuntimeType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing runtime type: " + name);
    }

    object Call(string method, params object[] args)
    {
        return boardType.GetMethod(method).Invoke(board, args);
    }

    GameObject Prefab(string name, string tag)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root.transform);
        obj.tag = tag;
        var token = obj.AddComponent(tokenType);
        tokenType.GetField("fallSpeed", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(token, 1000f);
        return obj;
    }

    [SetUp]
    public void SetUp()
    {
        var managerType = RuntimeType("GameManager");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "Run this isolated fixture without a live GameManager to avoid rank writes.");
        boardType = RuntimeType("BoardManager");
        tokenType = RuntimeType("Token");
        snapshotType = RuntimeType("KineticRank.Logic.BoardSnapshot");
        root = new GameObject("Board load fixture");
        board = root.AddComponent(boardType);
        // Prevent Start from generating tiles.
        ((Behaviour)board).enabled = false;
        boardType.GetField("columns").SetValue(board, Columns);
        boardType.GetField("rows").SetValue(board, Rows);
        boardType.GetField("boardParent").SetValue(board, root.transform);
        grid = new GameObject[Columns, Rows];
        boardType.GetField("tokenGrid", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(board, grid);
        red = Prefab("Red fixture template", "TokenRed");
        blue = Prefab("Blue fixture template", "TokenBlue");
        boardType.GetField("redTokenPrefab").SetValue(board, red);
        boardType.GetField("blueTokenPrefab").SetValue(board, blue);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
    }

    object Capture(Func<int, int, bool, bool> isToken, bool redToMove)
    {
        return snapshotType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { isToken, redToMove, true, true, true, true });
    }

    object State(string[] layout)
    {
        return Capture((x, y, isRed) =>
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return false;
            return layout[Rows - 1 - y][x] == (isRed ? 'R' : 'B');
        }, true);
    }

    object CaptureBoard()
    {
        return Capture((x, y, isRed) => (bool)Call("IsToken", x, y, isRed), true);
    }

    static int Cell(object state, int x, int y)
    {
        return (int)state.GetType().GetMethod("Cell").Invoke(state, new object[] { x, y });
    }

    static int CountTokens(string[] layout)
    {
        int count = 0;
        foreach (var row in layout)
            foreach (var c in row)
                if (c != '.') count++;
        return count;
    }

    int CountBoardTokens()
    {
        int count = 0;
        for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                if ((bool)Call("IsToken", x, y, true) || (bool)Call("IsToken", x, y, false)) count++;
        return count;
    }

    bool Load(object state)
    {
        return (bool)Call("LoadPosition", state);
    }

    void AssertLayoutState(string[] layout, object state)
    {
        for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                Assert.That(Cell(state, x, y) != 0, Is.EqualTo(layout[Rows - 1 - y][x] != '.'),
                    "Input state cell x=" + x + " y=" + y);
    }

    void AssertBoardMatches(object expected)
    {
        var actual = CaptureBoard();
        for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                Assert.That(Cell(actual, x, y), Is.EqualTo(Cell(expected, x, y)),
                    "Board cell x=" + x + " y=" + y);
    }

    [UnityTest]
    public IEnumerator MidGamePositionRoundTrips()
    {
        var expected = State(MidGame);
        AssertLayoutState(MidGame, expected);

        Assert.That(Load(expected), Is.True, "LoadPosition should accept a valid state");
        yield return null;

        AssertBoardMatches(expected);
        Assert.That(CountBoardTokens(), Is.EqualTo(CountTokens(MidGame)));
    }

    [UnityTest]
    public IEnumerator LoadingReplacesPreviousPositionDownToEmpty()
    {
        var full = State(FullColumn);
        Assert.That(Load(full), Is.True);
        yield return null;
        AssertBoardMatches(full);
        Assert.That(CountBoardTokens(), Is.EqualTo(CountTokens(FullColumn)));

        var empty = State(Empty);
        Assert.That(Load(empty), Is.True);
        yield return null;
        AssertBoardMatches(empty);
        Assert.That(CountBoardTokens(), Is.Zero);
    }

    [UnityTest]
    public IEnumerator LoadIsRejectedWhileMoveResolves()
    {
        var full = State(FullColumn);
        Assert.That(Load(full), Is.True);
        yield return null;

        var resolving = boardType.GetField("resolvingMove", BindingFlags.Instance | BindingFlags.NonPublic);
        resolving.SetValue(board, true);
        Assert.That(Load(State(MidGame)), Is.False, "LoadPosition must refuse while a move resolves");
        yield return null;
        resolving.SetValue(board, false);

        AssertBoardMatches(full);
        Assert.That(CountBoardTokens(), Is.EqualTo(CountTokens(FullColumn)));
    }
}
