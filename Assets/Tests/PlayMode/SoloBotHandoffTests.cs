using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Solo handoff check: a human move through PlayerController.TryPlace hands the turn to the bot, which replies once and then waits
public class SoloBotHandoffTests
{
    const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    GameObject root;
    Component board;
    Component player;
    Type boardType;
    Type playerType;
    Type settingsType;
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

    static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, AnyInstance);
        Assert.That(field, Is.Not.Null, "Missing field: " + target.GetType().Name + "." + name);
        field.SetValue(target, value);
    }

    GameObject Prefab(string name, string tag)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root.transform);
        obj.tag = tag;
        var token = obj.AddComponent(RuntimeType("Token"));
        SetField(token, "fallSpeed", 1000f);
        return obj;
    }

    [SetUp]
    public void SetUp()
    {
        var managerType = RuntimeType("GameManager");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "Run this isolated fixture without a live GameManager to avoid rank writes.");
        boardType = RuntimeType("BoardManager");
        playerType = RuntimeType("PlayerController");
        settingsType = RuntimeType("SoloSettings");

        root = new GameObject("Solo bot handoff fixture");
        board = root.AddComponent(boardType);
        // Prevent Start from generating tiles; coroutines still run on this active object.
        ((Behaviour)board).enabled = false;
        SetField(board, "columns", 7);
        SetField(board, "rows", 6);
        SetField(board, "boardParent", root.transform);
        grid = new GameObject[7, 6];
        SetField(board, "tokenGrid", grid);
        var red = Prefab("Red fixture template", "TokenRed");
        var blue = Prefab("Blue fixture template", "TokenBlue");
        SetField(board, "redTokenPrefab", red);
        SetField(board, "blueTokenPrefab", blue);

        // OnEnable assigns SoloBotDriver.MoveSink to this controller
        player = root.AddComponent(playerType);
        SetField(player, "board", board);
        SetField(player, "redTokenPrefab", red);
        SetField(player, "blueTokenPrefab", blue);
        SetField(player, "redQueenPrefab", red);
        SetField(player, "blueQueenPrefab", blue);
        SetField(player, "redCleanerPrefab", red);
        SetField(player, "blueCleanerPrefab", blue);

        StartSolo();

        var driver = root.AddComponent(RuntimeType("SoloBotDriver"));
        SetField(driver, "board", board);
        SetField(driver, "player", player);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var clear = settingsType.GetMethod("Clear", AnyStatic);
        Assert.That(clear, Is.Not.Null, "Missing SoloSettings.Clear");
        clear.Invoke(null, null);
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
        Assert.That(StaticFlag("Enabled"), Is.False, "Solo must not leak into other fixtures.");
    }

    bool StaticFlag(string name)
    {
        var prop = settingsType.GetProperty(name, AnyStatic);
        Assert.That(prop, Is.Not.Null, "Missing SoloSettings." + name);
        return (bool)prop.GetValue(null);
    }

    // Lowest tier, bot plays Blue so the human (Red) moves first
    void StartSolo()
    {
        var start = settingsType.GetMethod("StartSolo", AnyStatic);
        Assert.That(start, Is.Not.Null, "Missing SoloSettings.StartSolo");
        var parameters = start.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        var lowestTier = Enum.GetValues(parameters[0].ParameterType).GetValue(0);
        start.Invoke(null, new object[] { lowestTier, false });
        Assert.That(StaticFlag("Enabled"), Is.True);
        Assert.That(StaticFlag("BotPlaysRed"), Is.False);
    }

    // Human input path: PlayerController.TryPlace(column, PieceType.Normal)
    void HumanPlace(int column)
    {
        var tryPlace = playerType.GetMethod("TryPlace", AnyInstance);
        Assert.That(tryPlace, Is.Not.Null, "Missing PlayerController.TryPlace");
        var parameters = tryPlace.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        var normal = Enum.ToObject(parameters[1].ParameterType, 0);
        var result = tryPlace.Invoke(player, new object[] { column, normal });
        if (result is bool accepted) Assert.That(accepted, Is.True, "Human move was rejected.");
    }

    bool IsRedTurn => (bool)playerType.GetProperty("IsRedTurn").GetValue(player);

    bool Resolving => (bool)boardType.GetField("resolvingMove", AnyInstance).GetValue(board);

    int TokenCount()
    {
        int count = 0;
        foreach (var cell in grid) if (cell != null) count++;
        return count;
    }

    GameObject[,] Snapshot() => (GameObject[,])grid.Clone();

    bool GridDiffers(GameObject[,] before)
    {
        for (int c = 0; c < grid.GetLength(0); c++)
            for (int r = 0; r < grid.GetLength(1); r++)
                if (grid[c, r] != before[c, r]) return true;
        return false;
    }

    [UnityTest]
    public IEnumerator HumanMoveHandsTurnToBotWhichRepliesThenWaits()
    {
        Assert.That(IsRedTurn, Is.True);
        Assert.That(TokenCount(), Is.Zero);

        // Red is the human side; the bot must not open the game
        float idle = Time.realtimeSinceStartup + 0.5f;
        while (Time.realtimeSinceStartup < idle) yield return null;
        Assert.That(IsRedTurn, Is.True, "Bot moved on the human's opening turn.");
        Assert.That(TokenCount(), Is.Zero);

        HumanPlace(3);

        bool sawBotTurn = false;
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!sawBotTurn && Time.realtimeSinceStartup < deadline)
        {
            if (!IsRedTurn) sawBotTurn = true;
            else yield return null;
        }
        Assert.That(sawBotTurn, Is.True, "Human move did not hand the turn to the bot within 5 s.");
        while (Resolving && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Resolving, Is.False, "Human move did not finish resolving.");
        Assert.That(TokenCount(), Is.GreaterThan(0), "Human move placed no token in the grid.");
        var afterHuman = Snapshot();

        deadline = Time.realtimeSinceStartup + 5f;
        while ((!IsRedTurn || Resolving) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(IsRedTurn, Is.True, "Bot did not reply within 5 s.");
        Assert.That(Resolving, Is.False, "Bot reply did not finish resolving.");
        Assert.That(GridDiffers(afterHuman), Is.True, "Bot reply did not change the grid.");

        // Back on the human's turn; the bot must stay idle
        var afterBot = Snapshot();
        float hold = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < hold) yield return null;
        Assert.That(IsRedTurn, Is.True, "Bot must not move on the human's turn.");
        Assert.That(GridDiffers(afterBot), Is.False, "Grid changed during the human's turn.");
    }
}
