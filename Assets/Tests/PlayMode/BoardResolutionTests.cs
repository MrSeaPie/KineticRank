using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BoardResolutionTests
{
    GameObject root;
    Component board;
    Type boardType;
    Type tokenType;
    GameObject red;
    GameObject blue;
    GameObject[,] grid;

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
        root = new GameObject("Board resolution fixture");
        board = root.AddComponent(boardType);
        // Prevent Start from generating tiles; coroutines still run on this active object.
        ((Behaviour)board).enabled = false;
        boardType.GetField("columns").SetValue(board, 1);
        boardType.GetField("rows").SetValue(board, 3);
        boardType.GetField("boardParent").SetValue(board, root.transform);
        grid = new GameObject[1, 3];
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

    bool Resolving => (bool)boardType.GetField("resolvingMove",
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);

    IEnumerator WaitForResolution()
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (Resolving && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Resolving, Is.False, "Move coroutine did not complete.");
    }

    void AssertSettled(int row)
    {
        Assert.That(grid[0, row], Is.Not.Null);
        var token = grid[0, row].GetComponent(tokenType);
        Assert.That((bool)tokenType.GetProperty("HasArrived").GetValue(token), Is.True);
        Assert.That(Vector2.Distance(grid[0, row].transform.position,
            new Vector2(0.5f, row + 0.5f)), Is.LessThan(0.001f));
    }

    [UnityTest]
    public IEnumerator QueenSettlesReplacementsAndReopensAdmission()
    {
        Assert.That((bool)Call("PlaceNormalToken", 0, red, true), Is.True);
        yield return WaitForResolution();
        Assert.That((bool)Call("PlaceQueen", 0, red, red, true), Is.True);
        Assert.That((bool)Call("PlaceNormalToken", 0, blue, false), Is.False,
            "Admission must remain closed while the Queen resolves.");
        yield return WaitForResolution();
        Assert.That(grid[0, 0].CompareTag("TokenBlue"), Is.True);
        Assert.That(grid[0, 1].CompareTag("TokenRed"), Is.True);
        AssertSettled(0);
        AssertSettled(1);
        Assert.That((bool)Call("PlaceNormalToken", 0, blue, false), Is.True);
        yield return WaitForResolution();
        AssertSettled(2);
    }

    [UnityTest]
    public IEnumerator CleanerClearsColumnAndReopensAdmission()
    {
        Assert.That((bool)Call("PlaceNormalToken", 0, red, true), Is.True);
        yield return WaitForResolution();
        Assert.That((bool)Call("PlaceCleaner", 0, blue), Is.True);
        yield return WaitForResolution();
        for (int row = 0; row < 3; row++)
            Assert.That(grid[0, row] == null, Is.True);
        Assert.That((bool)Call("PlaceNormalToken", 0, red, true), Is.True);
        yield return WaitForResolution();
        AssertSettled(0);
    }

    [UnityTest]
    public IEnumerator FullColumnPopsBottomAndSettlesShiftedTokens()
    {
        foreach (var prefab in new[] { red, blue, red })
        {
            Assert.That((bool)Call("PlaceNormalToken", 0, prefab, prefab == red), Is.True);
            yield return WaitForResolution();
        }
        var bottom = grid[0, 0];
        var middle = grid[0, 1];
        var top = grid[0, 2];
        Assert.That((bool)Call("PlaceNormalToken", 0, blue, false), Is.True);
        yield return WaitForResolution();
        yield return null;
        Assert.That(bottom == null, Is.True, "The former bottom token must be destroyed.");
        Assert.That(grid[0, 0], Is.SameAs(middle));
        Assert.That(grid[0, 1], Is.SameAs(top));
        Assert.That(grid[0, 2].CompareTag("TokenBlue"), Is.True);
        for (int row = 0; row < 3; row++) AssertSettled(row);
    }
}
