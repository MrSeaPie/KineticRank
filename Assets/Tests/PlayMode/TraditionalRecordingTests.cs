using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Traditional mode is always casual: a finished hot-seat match must not touch SaveManager's record or Elo.
// Expected to fail until GameManager.FinishMatch gains its Traditional guard.
public class TraditionalRecordingTests
{
    GameObject root;
    Component manager;
    Type managerType;
    Type saveType;
    IDisposable isolation;

    // Exact name first, then a simple-name match in any KineticRank (or global) namespace
    static Type RuntimeType(string name)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var type = assemblies.Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
        if (type == null)
        {
            type = assemblies
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
                })
                .FirstOrDefault(t => t.Name == name && (t.Namespace == null || t.Namespace.StartsWith("KineticRank")));
        }
        if (type == null) throw new InvalidOperationException("Missing runtime type: " + name);
        return type;
    }

    static void SetTraditional(bool on)
    {
        RuntimeType("TraditionalSettings")
            .GetMethod(on ? "StartTraditional" : "Clear", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, null);
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
        SetTraditional(true);
        root = new GameObject("Traditional recording fixture");
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
        SetTraditional(false);
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
            var token = new GameObject("Traditional fixture token");
            token.tag = tag;
            token.transform.SetParent(root.transform);
            grid[column, row] = token;
        }
    }

    void CompleteAndAssertNothingRecorded()
    {
        int redElo = (int)Save("GetRedElo");
        int blueElo = (int)Save("GetBlueElo");
        Assert.That((bool)Call("TryBeginMove"), Is.True);
        Call("CompleteMove");
        Assert.That((bool)managerType.GetProperty("IsMatchOver").GetValue(manager), Is.True);
        Assert.That((int)Save("GetGames"), Is.Zero, "games");
        Assert.That((int)Save("GetRedWins"), Is.Zero, "red wins");
        Assert.That((int)Save("GetBlueWins"), Is.Zero, "blue wins");
        Assert.That((int)Save("GetDraws"), Is.Zero, "draws");
        Assert.That((int)Save("GetRedElo"), Is.EqualTo(redElo), "red Elo");
        Assert.That((int)Save("GetBlueElo"), Is.EqualTo(blueElo), "blue Elo");
    }

    [Test]
    public void RedWinRecordsNothing()
    {
        AddLine(0, "TokenRed");
        CompleteAndAssertNothingRecorded();
    }

    [Test]
    public void BlueWinRecordsNothing()
    {
        AddLine(0, "TokenBlue");
        CompleteAndAssertNothingRecorded();
    }

    [Test]
    public void DrawRecordsNothing()
    {
        AddLine(0, "TokenRed");
        AddLine(1, "TokenBlue");
        CompleteAndAssertNothingRecorded();
    }
}
