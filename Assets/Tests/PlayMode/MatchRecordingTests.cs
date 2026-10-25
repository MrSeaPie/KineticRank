using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MatchRecordingTests
{
    GameObject root;
    Component manager;
    Type managerType;
    Type saveType;
    IDisposable isolation;

    static Type RuntimeType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing runtime type: " + name);
    }

    object Save(string method)
    {
        return saveType.GetMethod(method).Invoke(null, null);
    }

    object Call(string method)
    {
        return managerType.GetMethod(method).Invoke(manager, null);
    }

    [SetUp]
    public void SetUp()
    {
        managerType = RuntimeType("GameManager");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "This fixture requires an isolated test scene.");
        saveType = RuntimeType("SaveManager");
        isolation = (IDisposable)Save("BeginIsolatedTestScope");
        root = new GameObject("Match recording fixture");
        root.SetActive(false);
        var boardType = RuntimeType("BoardManager");
        var board = root.AddComponent(boardType);
        ((Behaviour)board).enabled = false;
        boardType.GetField("columns").SetValue(board, 4);
        boardType.GetField("rows").SetValue(board, 2);
        boardType.GetField("tokenGrid", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(board, new GameObject[4, 2]);
        manager = root.AddComponent(managerType);
        ((Behaviour)manager).enabled = false;
        managerType.GetField("board").SetValue(manager, board);
        root.SetActive(true);
        Assert.That((int)Save("GetGames"), Is.Zero);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
        isolation?.Dispose();
        isolation = null;
    }

    void AddLine(int row, string tag)
    {
        var board = (Component)managerType.GetField("board").GetValue(manager);
        var grid = (GameObject[,])board.GetType()
            .GetField("tokenGrid", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
        for (int column = 0; column < 4; column++)
        {
            var token = new GameObject("Match fixture token");
            token.tag = tag;
            token.transform.SetParent(root.transform);
            grid[column, row] = token;
        }
    }

    void CompleteAndAssert(string counter)
    {
        Assert.That((bool)Call("TryBeginMove"), Is.True);
        Call("CompleteMove");
        Assert.That((bool)managerType.GetProperty("IsMatchOver").GetValue(manager), Is.True);
        Assert.That((bool)managerType.GetProperty("IsInputLocked").GetValue(manager), Is.True);
        Assert.That((int)Save("GetGames"), Is.EqualTo(1));
        Assert.That((int)Save(counter), Is.EqualTo(1));
        int redElo = (int)Save("GetRedElo");
        int blueElo = (int)Save("GetBlueElo");
        Call("CompleteMove");
        Call("CompleteMove");
        Assert.That((bool)Call("TryBeginMove"), Is.False);
        Assert.That((int)Save("GetGames"), Is.EqualTo(1));
        Assert.That((int)Save(counter), Is.EqualTo(1));
        Assert.That((int)Save("GetRedElo"), Is.EqualTo(redElo));
        Assert.That((int)Save("GetBlueElo"), Is.EqualTo(blueElo));
    }

    [Test]
    public void RedLineRecordsExactlyOneWin()
    {
        AddLine(0, "TokenRed");
        CompleteAndAssert("GetRedWins");
        Assert.That((int)Save("GetBlueWins"), Is.Zero);
        Assert.That((int)Save("GetDraws"), Is.Zero);
    }

    [Test]
    public void BlueLineRecordsExactlyOneWin()
    {
        AddLine(0, "TokenBlue");
        CompleteAndAssert("GetBlueWins");
        Assert.That((int)Save("GetRedWins"), Is.Zero);
        Assert.That((int)Save("GetDraws"), Is.Zero);
    }

    [Test]
    public void SimultaneousLinesRecordExactlyOneDraw()
    {
        AddLine(0, "TokenRed");
        AddLine(1, "TokenBlue");
        CompleteAndAssert("GetDraws");
        Assert.That((int)Save("GetRedWins"), Is.Zero);
        Assert.That((int)Save("GetBlueWins"), Is.Zero);
    }
}
