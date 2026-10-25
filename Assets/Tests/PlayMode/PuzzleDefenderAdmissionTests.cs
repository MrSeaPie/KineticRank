using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Puzzle defender admission: SubmitPuzzleReply only accepts on the defender's turn, and the human-input guard
// (IsPuzzleDefenderTurn) is evaluated at input time so it survives HowToPlayOverlay re-enabling the player
public class PuzzleDefenderAdmissionTests
{
    const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const int Column = 3;

    GameObject root;
    Component board;
    Component player;
    Type playerType;
    Type puzzleType;
    Type soloType;
    GameObject[,] grid;

    static Type RuntimeType(params string[] names)
    {
        foreach (var name in names)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
        throw new InvalidOperationException("Missing runtime type: " + string.Join(" / ", names));
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
        var boardType = RuntimeType("BoardManager");
        playerType = RuntimeType("PlayerController");
        puzzleType = RuntimeType("KineticRank.Logic.PuzzleSettings", "PuzzleSettings");
        soloType = RuntimeType("SoloSettings", "KineticRank.Logic.SoloSettings");

        root = new GameObject("Puzzle defender fixture");
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

        player = root.AddComponent(playerType);
        SetField(player, "board", board);
        SetField(player, "redTokenPrefab", red);
        SetField(player, "blueTokenPrefab", blue);
        SetField(player, "redQueenPrefab", red);
        SetField(player, "blueQueenPrefab", blue);
        SetField(player, "redCleanerPrefab", red);
        SetField(player, "blueCleanerPrefab", blue);

        // Red attacks, so Blue is the defender
        var start = puzzleType.GetMethod("StartPuzzle", AnyStatic);
        Assert.That(start, Is.Not.Null, "Missing PuzzleSettings.StartPuzzle");
        start.Invoke(null, new object[] { "test_puzzle", true });
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        puzzleType.GetMethod("Clear", AnyStatic).Invoke(null, null);
        soloType.GetMethod("Clear", AnyStatic).Invoke(null, null);
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
        Assert.That(StaticFlag(puzzleType, "Enabled"), Is.False, "Puzzle must not leak into other fixtures.");
        Assert.That(StaticFlag(soloType, "Enabled"), Is.False, "Solo must not leak into other fixtures.");
    }

    static bool StaticFlag(Type type, string name)
    {
        var prop = type.GetProperty(name, AnyStatic);
        Assert.That(prop, Is.Not.Null, "Missing " + type.Name + "." + name);
        return (bool)prop.GetValue(null);
    }

    void SetRedTurn(bool red) => SetField(player, "isRedTurn", red);

    bool IsRedTurn => (bool)playerType.GetField("isRedTurn", AnyInstance).GetValue(player);

    bool DefenderTurn
    {
        get
        {
            var prop = playerType.GetProperty("IsPuzzleDefenderTurn", AnyInstance);
            Assert.That(prop, Is.Not.Null, "Missing PlayerController.IsPuzzleDefenderTurn");
            return (bool)prop.GetValue(player);
        }
    }

    // Normal move kind, whatever the enum calls it (SubmitBotMove maps anything but Queen/Cleaner to Normal)
    static object NormalKind(Type kindType)
    {
        foreach (var value in Enum.GetValues(kindType))
        {
            var name = value.ToString();
            if (name != "Queen" && name != "Cleaner") return value;
        }
        throw new InvalidOperationException("MoveKind has no normal value");
    }

    bool Submit(string method, int col)
    {
        var submit = playerType.GetMethod(method, AnyInstance);
        Assert.That(submit, Is.Not.Null, "Missing PlayerController." + method);
        var kindType = submit.GetParameters()[1].ParameterType;
        return (bool)submit.Invoke(player, new object[] { col, NormalKind(kindType) });
    }

    bool ColumnHasToken(int col)
    {
        for (int row = 0; row < grid.GetLength(1); row++)
            if (grid[col, row] != null) return true;
        return false;
    }

    static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    [UnityTest]
    public IEnumerator DefenderReply_IsAcceptedOnDefenderTurn()
    {
        SetRedTurn(false);
        Assert.That(DefenderTurn, Is.True);

        Assert.That(Submit("SubmitPuzzleReply", Column), Is.True, "Defender reply should be admitted on Blue's turn");
        yield return Frames(3);
        Assert.That(ColumnHasToken(Column), Is.True, "Defender reply should place a token");
    }

    [UnityTest]
    public IEnumerator DefenderReply_IsRejectedOnAttackerTurn()
    {
        SetRedTurn(true);
        Assert.That(DefenderTurn, Is.False);

        Assert.That(Submit("SubmitPuzzleReply", Column), Is.False, "Defender reply must not move the attacker's side");
        yield return Frames(3);
        Assert.That(ColumnHasToken(Column), Is.False);
        Assert.That(IsRedTurn, Is.True, "Rejected reply must not flip the turn");
    }

    [UnityTest]
    public IEnumerator DefenderReply_IsRejectedOutsidePuzzleMode()
    {
        puzzleType.GetMethod("Clear", AnyStatic).Invoke(null, null);
        SetRedTurn(false);
        Assert.That(DefenderTurn, Is.False);

        Assert.That(Submit("SubmitPuzzleReply", Column), Is.False);
        yield return Frames(3);
        Assert.That(ColumnHasToken(Column), Is.False);
    }

    // HowToPlayOverlay disables the player, then re-enables it one frame after closing; the guard must still hold
    [UnityTest]
    public IEnumerator HumanGuard_HoldsAfterOverlayRestore()
    {
        SetRedTurn(false);
        ((Behaviour)player).enabled = false;
        yield return null;
        ((Behaviour)player).enabled = true;
        yield return Frames(2);

        Assert.That(((Behaviour)player).enabled, Is.True);
        Assert.That(DefenderTurn, Is.True, "Input-time guard must still block human input on the defender's turn");
        Assert.That(IsRedTurn, Is.False, "No move should have been made for the defender");
        Assert.That(ColumnHasToken(Column), Is.False);
    }

    [UnityTest]
    public IEnumerator SoloBotEntry_StillRejectsPuzzleDefender()
    {
        SetRedTurn(false);
        Assert.That(StaticFlag(soloType, "Enabled"), Is.False);

        Assert.That(Submit("SubmitBotMove", Column), Is.False, "SubmitBotMove stays Solo-only");
        yield return Frames(3);
        Assert.That(ColumnHasToken(Column), Is.False);
    }
}
