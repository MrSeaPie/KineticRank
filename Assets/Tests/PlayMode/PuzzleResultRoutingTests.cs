using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Puzzle results must record PuzzleProgress only, never ranked Elo or match counters, and the solo bot
// must stay out of puzzles. Uses the same isolated board fixture as MatchRecordingTests.
public class PuzzleResultRoutingTests
{
    const string PuzzleId = "test_puzzle_routing";

    GameObject root;
    GameObject botRoot;
    Component manager;
    Type managerType;
    Type saveType;
    Type puzzleSettingsType;
    Type puzzleProgressType;
    Type soloSettingsType;
    IDisposable isolation;

    static Type RuntimeType(params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
        }
        throw new InvalidOperationException("Missing runtime type: " + string.Join(" / ", names));
    }

    object Save(string method)
    {
        return saveType.GetMethod(method).Invoke(null, null);
    }

    object Call(string method)
    {
        return managerType.GetMethod(method).Invoke(manager, null);
    }

    bool IsSolved(string id)
    {
        return (bool)puzzleProgressType.GetMethod("IsSolved").Invoke(null, new object[] { id });
    }

    void ClearPuzzleState()
    {
        puzzleSettingsType.GetMethod("Clear").Invoke(null, null);
        puzzleProgressType.GetMethod("ClearSolved").Invoke(null, new object[] { PuzzleId });
        soloSettingsType.GetMethod("Clear").Invoke(null, null);
    }

    void StartPuzzle()
    {
        puzzleSettingsType.GetMethod("StartPuzzle").Invoke(null, new object[] { PuzzleId, true });
    }

    [SetUp]
    public void SetUp()
    {
        managerType = RuntimeType("GameManager");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "This fixture requires an isolated test scene.");
        saveType = RuntimeType("SaveManager");
        puzzleSettingsType = RuntimeType("PuzzleSettings", "KineticRank.Logic.PuzzleSettings");
        puzzleProgressType = RuntimeType("PuzzleProgress", "KineticRank.Logic.PuzzleProgress");
        soloSettingsType = RuntimeType("SoloSettings");
        ClearPuzzleState();
        isolation = (IDisposable)Save("BeginIsolatedTestScope");
        root = new GameObject("Puzzle routing fixture");
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
        Assert.That(IsSolved(PuzzleId), Is.False);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (root) UnityEngine.Object.Destroy(root);
        if (botRoot) UnityEngine.Object.Destroy(botRoot);
        yield return null;
        isolation?.Dispose();
        isolation = null;
        ClearPuzzleState();
    }

    void AddLine(int row, string tag)
    {
        var board = (Component)managerType.GetField("board").GetValue(manager);
        var grid = (GameObject[,])board.GetType()
            .GetField("tokenGrid", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
        for (int column = 0; column < 4; column++)
        {
            var token = new GameObject("Puzzle fixture token");
            token.tag = tag;
            token.transform.SetParent(root.transform);
            grid[column, row] = token;
        }
    }

    void CompleteAndAssertRankedUnchanged()
    {
        int redElo = (int)Save("GetRedElo");
        int blueElo = (int)Save("GetBlueElo");
        Assert.That((bool)Call("TryBeginMove"), Is.True);
        Call("CompleteMove");
        Assert.That((bool)managerType.GetProperty("IsMatchOver").GetValue(manager), Is.True);
        Assert.That((int)Save("GetGames"), Is.Zero);
        Assert.That((int)Save("GetRedWins"), Is.Zero);
        Assert.That((int)Save("GetBlueWins"), Is.Zero);
        Assert.That((int)Save("GetDraws"), Is.Zero);
        Assert.That((int)Save("GetRedElo"), Is.EqualTo(redElo));
        Assert.That((int)Save("GetBlueElo"), Is.EqualTo(blueElo));
    }

    [Test]
    public void AttackerLineSolvesPuzzleWithoutRankedRecord()
    {
        StartPuzzle();
        AddLine(0, "TokenRed");
        CompleteAndAssertRankedUnchanged();
        Assert.That(IsSolved(PuzzleId), Is.True);
    }

    [Test]
    public void DefenderLineLeavesPuzzleUnsolvedWithoutRankedRecord()
    {
        StartPuzzle();
        AddLine(0, "TokenBlue");
        CompleteAndAssertRankedUnchanged();
        Assert.That(IsSolved(PuzzleId), Is.False);
    }

    [Test]
    public void SoloBotStaysInactiveDuringPuzzle()
    {
        // Inactive host so neither Awake nor Update runs; only the IsBotTurn gate is under test
        botRoot = new GameObject("Puzzle routing bot");
        botRoot.SetActive(false);
        var playerType = RuntimeType("PlayerController");
        var driverType = RuntimeType("SoloBotDriver");
        var player = botRoot.AddComponent(playerType);
        var driver = botRoot.AddComponent(driverType);
        driverType.GetField("player").SetValue(driver, player);

        bool redTurn = (bool)playerType.GetProperty("IsRedTurn").GetValue(player);
        var startSolo = soloSettingsType.GetMethod("StartSolo");
        var tierType = startSolo.GetParameters()[0].ParameterType;
        startSolo.Invoke(null, new object[] { Enum.GetValues(tierType).GetValue(0), redTurn });
        var isBotTurn = driverType.GetProperty("IsBotTurn");
        Assert.That((bool)isBotTurn.GetValue(driver), Is.True, "Control: the bot should own this turn in Solo.");

        StartPuzzle();
        Assert.That((bool)isBotTurn.GetValue(driver), Is.False);
    }
}
