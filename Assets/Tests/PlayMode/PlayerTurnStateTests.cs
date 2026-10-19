using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerTurnStateTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject root;
    Component player;
    Type playerType;
    Type stateType;

    static Type RuntimeType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing runtime type: " + name);
    }

    [SetUp]
    public void SetUp()
    {
        var managerType = RuntimeType("GameManager");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "Run this isolated fixture without a live GameManager to avoid rank writes.");
        playerType = RuntimeType("PlayerController");
        stateType = RuntimeType("KineticRank.Logic.GameState");
        root = new GameObject("Turn state fixture");
        // Keep the object inactive so Awake/Start/Update never run without a board.
        root.SetActive(false);
        player = root.AddComponent(playerType);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
    }

    object State(bool redToMove, bool redQueen, bool redCleaner, bool blueQueen, bool blueCleaner)
    {
        var rows = (int)stateType.GetProperty("Rows").GetValue(null);
        var columns = (int)stateType.GetProperty("Columns").GetValue(null);
        return stateType.GetMethod("FromGrid", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { new int[columns, rows], redToMove, redQueen, redCleaner, blueQueen, blueCleaner });
    }

    void Load(object state)
    {
        playerType.GetMethod("LoadTurnState").Invoke(player, new[] { state });
    }

    bool Available(string method, bool red)
    {
        return (bool)playerType.GetMethod(method).Invoke(player, new object[] { red });
    }

    bool RedTurn => (bool)playerType.GetField("isRedTurn", Private).GetValue(player);

    string CurrentPiece => playerType.GetField("currentPiece", Private).GetValue(player).ToString();

    void SetPrivate(string field, object value)
    {
        playerType.GetField(field, Private).SetValue(player, value);
    }

    [Test]
    public void LoadTurnState_AppliesSideToMoveAndUsedPowerUps()
    {
        var pieceField = playerType.GetField("currentPiece", Private);
        pieceField.SetValue(player, Enum.Parse(pieceField.FieldType, "Queen"));

        Load(State(false, false, true, true, false));

        Assert.That(RedTurn, Is.False, "Blue should move first.");
        Assert.That(Available("QueenAvailable", true), Is.False);
        Assert.That(Available("CleanerAvailable", true), Is.True);
        Assert.That(Available("QueenAvailable", false), Is.True);
        Assert.That(Available("CleanerAvailable", false), Is.False, "TrySelect must reject Blue's used Cleaner.");
        Assert.That(CurrentPiece, Is.EqualTo("Normal"));
    }

    [Test]
    public void LoadTurnState_InitialRestoresEveryPowerUpAndRedToMove()
    {
        SetPrivate("isRedTurn", false);
        SetPrivate("redQueenUsed", true);
        SetPrivate("redCleanerUsed", true);
        SetPrivate("blueQueenUsed", true);
        SetPrivate("blueCleanerUsed", true);

        Load(stateType.GetMethod("Initial", BindingFlags.Public | BindingFlags.Static).Invoke(null, null));

        Assert.That(RedTurn, Is.True);
        Assert.That(Available("QueenAvailable", true), Is.True);
        Assert.That(Available("CleanerAvailable", true), Is.True);
        Assert.That(Available("QueenAvailable", false), Is.True);
        Assert.That(Available("CleanerAvailable", false), Is.True);
    }

    [Test]
    public void LoadTurnState_NullLeavesStateUnchanged()
    {
        SetPrivate("isRedTurn", false);
        SetPrivate("blueQueenUsed", true);

        Load(null);

        Assert.That(RedTurn, Is.False);
        Assert.That(Available("QueenAvailable", false), Is.False);
        Assert.That(Available("QueenAvailable", true), Is.True);
    }
}
