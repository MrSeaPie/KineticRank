using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// End-to-end Solo check: the bot's move goes through SoloBotDriver -> MoveSink -> PlayerController.SubmitBotMove -> BoardManager
public class SoloBotReplyTests
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

        root = new GameObject("Solo bot reply fixture");
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
        StopSolo();
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

    // Lowest tier, bot plays Red so it moves first
    void StartSolo()
    {
        var start = settingsType.GetMethod("StartSolo", AnyStatic);
        Assert.That(start, Is.Not.Null, "Missing SoloSettings.StartSolo");
        var parameters = start.GetParameters();
        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var type = parameters[i].ParameterType;
            if (type.IsEnum) args[i] = Enum.GetValues(type).GetValue(0);
            else if (type == typeof(bool)) args[i] = true;
            else if (type == typeof(int)) args[i] = 1;
            else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
            else Assert.Fail("Unexpected StartSolo parameter: " + parameters[i].Name);
        }
        start.Invoke(null, args);
        Assert.That(StaticFlag("Enabled"), Is.True);
        Assert.That(StaticFlag("BotPlaysRed"), Is.True);
    }

    void StopSolo()
    {
        foreach (var name in new[] { "Stop", "StopSolo", "EndSolo", "Reset", "Clear", "Disable" })
        {
            var method = settingsType.GetMethod(name, AnyStatic, null, Type.EmptyTypes, null);
            if (method != null) { method.Invoke(null, null); return; }
        }
        var setter = settingsType.GetProperty("Enabled", AnyStatic)?.GetSetMethod(true);
        if (setter != null) { setter.Invoke(null, new object[] { false }); return; }
        var field = settingsType.GetField("<Enabled>k__BackingField", AnyStatic)
                 ?? settingsType.GetField("enabled", AnyStatic);
        field?.SetValue(null, false);
    }

    bool IsRedTurn => (bool)playerType.GetProperty("IsRedTurn").GetValue(player);

    bool Resolving => (bool)boardType.GetField("resolvingMove", AnyInstance).GetValue(board);

    int TokenCount()
    {
        int count = 0;
        foreach (var cell in grid) if (cell != null) count++;
        return count;
    }

    [UnityTest]
    public IEnumerator BotMovesFirstThroughPlayerControllerThenWaitsForHuman()
    {
        Assert.That(IsRedTurn, Is.True);
        Assert.That(TokenCount(), Is.Zero);

        float deadline = Time.realtimeSinceStartup + 5f;
        while ((IsRedTurn || Resolving) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(IsRedTurn, Is.False, "Bot did not submit a move within 5 s.");
        Assert.That(Resolving, Is.False, "Bot move did not finish resolving.");
        Assert.That(TokenCount(), Is.GreaterThan(0), "Bot move placed no token in the grid.");

        // Blue is the human side; the bot must stay idle on its turn
        int placed = TokenCount();
        float hold = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < hold) yield return null;
        Assert.That(IsRedTurn, Is.False, "Bot must not move on the human's turn.");
        Assert.That(TokenCount(), Is.EqualTo(placed));
    }
}
