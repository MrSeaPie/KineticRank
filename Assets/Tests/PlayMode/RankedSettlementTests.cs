using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RankedSettlementTests
{
    GameObject root;
    IDisposable rankScope;
    Type managerType;
    Type storeType;
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    static Type RuntimeType(string name)
    {
        Type match = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }
            foreach (var type in types)
            {
                if (type == null || type.Name != name) continue;
                if (match != null && match != type)
                    throw new InvalidOperationException("Ambiguous runtime type: " + name);
                match = type;
            }
        }
        if (match == null) throw new InvalidOperationException("Missing runtime type: " + name);
        return match;
    }

    static void ClearSettings()
    {
        foreach (var name in new[] { "SoloSettings", "PuzzleSettings", "TraditionalSettings" })
            RuntimeType(name).GetMethod("Clear").Invoke(null, null);
    }

    object Profile() => storeType.GetMethod("GetProfile").Invoke(null, null);
    static int Field(object profile, string name) => (int)profile.GetType().GetField(name).GetValue(profile);

    [SetUp]
    public void SetUp()
    {
        managerType = RuntimeType("GameManager");
        storeType = RuntimeType("PlayerRankStore");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "Settlement tests require an isolated scene.");
        ClearSettings();
        rankScope = (IDisposable)storeType.GetMethod("BeginIsolatedTestScope").Invoke(null, null);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (root) UnityEngine.Object.DestroyImmediate(root);
            ClearSettings();
        }
        finally { rankScope?.Dispose(); }
    }

    [TestCase("RedWin", true)]
    [TestCase("BlueWin", false)]
    [TestCase("Draw", null)]
    public void RankedBoardResultPersistsOnceAfterMenuSettingsClear(string resultName, bool? humanWon)
    {
        var solo = RuntimeType("SoloSettings");
        var start = solo.GetMethod("StartRankedSolo");
        var tier = Enum.Parse(start.GetParameters()[0].ParameterType, "Pine");
        start.Invoke(null, new object[] { tier, false });
        var before = Profile();
        root = new GameObject("Ranked settlement fixture");
        root.SetActive(false);
        var manager = root.AddComponent(managerType);
        ((Behaviour)manager).enabled = false;
        root.SetActive(true);
        ClearSettings();

        var finish = managerType.GetMethod("FinishMatch", PrivateInstance);
        var resultType = finish.GetParameters()[0].ParameterType;
        finish.Invoke(manager, new[] { Enum.Parse(resultType, resultName) });
        var after = Profile();
        Assert.That(Field(after, "gamesPlayed"), Is.EqualTo(Field(before, "gamesPlayed") + 1));
        if (humanWon == true)
            Assert.That(Field(after, "rating"), Is.GreaterThan(Field(before, "rating")));
        else if (humanWon == false)
            Assert.That(Field(after, "rating"), Is.LessThan(Field(before, "rating")));
        Assert.That((bool)managerType.GetProperty("IsMatchOver").GetValue(manager), Is.True);

        finish.Invoke(manager, new[] { Enum.Parse(resultType, "RedWin") });
        managerType.GetMethod("CompleteMove").Invoke(manager, null);
        managerType.GetMethod("OnTokenPlaced").Invoke(manager, new object[] { 0, 0, true });
        var repeated = Profile();
        Assert.That(Field(repeated, "gamesPlayed"), Is.EqualTo(Field(after, "gamesPlayed")));
        Assert.That(Field(repeated, "rating"), Is.EqualTo(Field(after, "rating")));
    }
}
