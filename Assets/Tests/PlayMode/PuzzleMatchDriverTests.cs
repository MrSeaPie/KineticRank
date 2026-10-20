using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PuzzleMatchDriverTests
{
    const int Columns = 7, Rows = 6;
    const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string SettingsName = "KineticRank.Logic.PuzzleSettings";
    GameObject root;
    Component board;
    Component player;
    Component driver;
    Type boardType;
    Type playerType;
    Type driverType;
    Type tokenType;
    Type snapshotType;
    GameObject red;
    GameObject blue;

    // Rows are listed top-down: 'R' red, 'B' blue, '.' empty.
    // Red to move: only a drop in column 3 completes the bottom row; every other column is off the line.
    static readonly string[] MateInOne =
    {
        ".......",
        ".......",
        ".......",
        ".......",
        "BBB....",
        "RRR....",
    };

    // Full names first; falls back to the short type name so namespaced types still resolve.
    static Type RuntimeType(string name)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            foreach (var type in types)
                if (type != null && type.Name == name) return type;
        }
        throw new InvalidOperationException("Missing runtime type: " + name);
    }

    static object CallStatic(string typeName, string method, params object[] args)
    {
        return RuntimeType(typeName).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    }

    // Cleanup must not stop at the first failure, or a leaked board breaks later fixtures.
    static void TryCallStatic(string typeName, string method)
    {
        try { CallStatic(typeName, method); }
        catch (Exception e) { Debug.LogWarning("Cleanup " + typeName + "." + method + " failed: " + e.Message); }
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
        playerType = RuntimeType("PlayerController");
        driverType = RuntimeType("PuzzleMatchDriver");
        tokenType = RuntimeType("Token");
        snapshotType = RuntimeType("KineticRank.Logic.BoardSnapshot");
        root = new GameObject("Puzzle driver fixture");
        board = root.AddComponent(boardType);
        // Prevent Start from generating tiles.
        ((Behaviour)board).enabled = false;
        boardType.GetField("columns").SetValue(board, Columns);
        boardType.GetField("rows").SetValue(board, Rows);
        boardType.GetField("boardParent").SetValue(board, root.transform);
        boardType.GetField("tokenGrid", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(board, new GameObject[Columns, Rows]);
        red = Prefab("Red fixture template", "TokenRed");
        blue = Prefab("Blue fixture template", "TokenBlue");
        boardType.GetField("redTokenPrefab").SetValue(board, red);
        boardType.GetField("blueTokenPrefab").SetValue(board, blue);

        var playerObject = new GameObject("Player fixture");
        playerObject.transform.SetParent(root.transform);
        player = playerObject.AddComponent(playerType);
        // No mouse or key polling; moves go through TryPlace below.
        ((Behaviour)player).enabled = false;
        // Wire the board and every token prefab slot to the plain templates; these puzzles use normal drops only.
        foreach (var field in playerType.GetFields(AnyInstance))
        {
            if (field.FieldType == boardType) field.SetValue(player, board);
            else if (field.FieldType == typeof(GameObject))
            {
                var name = field.Name.ToLowerInvariant();
                if (name.Contains("red")) field.SetValue(player, red);
                else if (name.Contains("blue")) field.SetValue(player, blue);
            }
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        TryCallStatic("PuzzleMatchDriver", "Clear");
        TryCallStatic(SettingsName, "Clear");
        TryCallStatic("SoloSettings", "Clear");
        if (driverType != null)
            foreach (var live in UnityEngine.Object.FindObjectsOfType(driverType))
                UnityEngine.Object.Destroy(((Component)live).gameObject);
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
    }

    static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    object State(string[] layout)
    {
        Func<int, int, bool, bool> isToken = (x, y, isRed) =>
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return false;
            return layout[Rows - 1 - y][x] == (isRed ? 'R' : 'B');
        };
        // Red to move, power-ups spent so the only winning move is the normal drop.
        return snapshotType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { isToken, true, false, false, false, false });
    }

    IEnumerator StartDriver(string[] layout, int movesN)
    {
        CallStatic(SettingsName, "StartPuzzle", "driver-test", true);
        CallStatic("PuzzleMatchDriver", "Begin", State(layout), movesN);
        driver = root.AddComponent(driverType);
        yield return Frames(3);
    }

    // Plays an attacker drop the same way input does, so MovePlaced fires and the turn passes.
    IEnumerator Drop(int column)
    {
        var tryPlace = playerType.GetMethod("TryPlace", AnyInstance);
        var parameters = tryPlace.GetParameters();
        var args = new object[parameters.Length];
        args[0] = column;
        for (int i = 1; i < parameters.Length; i++)
        {
            var type = parameters[i].ParameterType;
            args[i] = type.IsEnum ? Enum.ToObject(type, 0) : type.IsValueType ? Activator.CreateInstance(type) : null;
        }
        tryPlace.Invoke(player, args);
        yield return Frames(10);
    }

    bool AttemptFailed => (bool)driverType.GetProperty("AttemptFailed").GetValue(driver);
    object Session => driverType.GetProperty("Session").GetValue(driver);

    bool IsRedAt(int x, int y) => (bool)boardType.GetMethod("IsToken").Invoke(board, new object[] { x, y, true });

    string Describe()
    {
        var session = Session;
        if (session == null) return "Session is null";
        var text = "Session:";
        foreach (var property in session.GetType().GetProperties())
            text += " " + property.Name + "=" + property.GetValue(session);
        return text;
    }

    // PuzzleSession.Status is PuzzleStatus { InProgress, Solved, Failed }.
    bool IsSolved()
    {
        var session = Session;
        Assert.IsNotNull(session, "Session is null");
        return session.GetType().GetProperty("Status").GetValue(session).ToString() == "Solved";
    }

    [UnityTest]
    public IEnumerator DriverLoadsStartPosition()
    {
        yield return StartDriver(MateInOne, 1);
        Assert.IsNotNull(Session);
        Assert.IsTrue(IsRedAt(0, 0));
        Assert.IsTrue(IsRedAt(2, 0));
        Assert.IsFalse(IsRedAt(3, 0));
        Assert.IsFalse(AttemptFailed);
    }

    [UnityTest]
    public IEnumerator CorrectMoveSolvesPuzzle()
    {
        yield return StartDriver(MateInOne, 1);
        yield return Drop(3);
        Assert.IsTrue(IsRedAt(3, 0), "The winning drop should land in column 3.");
        Assert.IsFalse(AttemptFailed, Describe());
        Assert.IsTrue(IsSolved(), Describe());
    }

    [UnityTest]
    public IEnumerator OffLineMoveFailsAndRetryReloads()
    {
        yield return StartDriver(MateInOne, 1);
        yield return Drop(6);
        Assert.IsTrue(AttemptFailed, Describe());
        var retry = driverType.GetMethod("Retry", AnyInstance);
        Assert.IsNotNull(retry, "PuzzleMatchDriver.Retry not found");
        retry.Invoke(driver, null);
        // The off-line drop may still be resolving; Update reloads only once the board is idle.
        var loadedField = driverType.GetField("loaded", AnyInstance);
        Assert.IsNotNull(loadedField, "PuzzleMatchDriver.loaded not found");
        var deadline = Time.realtimeSinceStartup + 5f;
        while (!(bool)loadedField.GetValue(driver) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsTrue((bool)loadedField.GetValue(driver), "Retry did not reload within 5s. " + Describe());
        yield return Frames(3);
        Assert.IsFalse(AttemptFailed, Describe());
        Assert.IsFalse(IsRedAt(6, 0), "Retry should remove the off-line drop.");
        Assert.IsTrue(IsRedAt(0, 0), "Retry should reload the start position.");
    }
}
