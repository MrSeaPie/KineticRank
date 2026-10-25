using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayAgainControlsTests
{
    const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    GameObject root;
    Type managerType;
    Type controlsType;
    Component manager;
    Component controls;
    FieldInfo singleton;
    object previousManager;

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
        managerType = RuntimeType("GameManager");
        controlsType = RuntimeType("PlayAgainControls");
        singleton = managerType.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        previousManager = singleton.GetValue(null);
        root = new GameObject("Restart admission fixture");
        root.SetActive(false);
        manager = root.AddComponent(managerType);
        controls = root.AddComponent(controlsType);
        singleton.SetValue(null, manager);
        controlsType.GetField("enableEnterToRestart", AnyInstance).SetValue(controls, true);
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        singleton?.SetValue(null, previousManager);
    }

    void SetState(bool matchOver, bool resolving)
    {
        managerType.GetProperty("IsMatchOver").GetSetMethod(true).Invoke(manager, new object[] { matchOver });
        managerType.GetProperty("IsResolvingMove").GetSetMethod(true).Invoke(manager, new object[] { resolving });
    }

    bool CanRestart()
    {
        var method = controlsType.GetMethod("CanRestartMatch", AnyInstance);
        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(controls, null);
    }

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void RestartRequiresCompletedIdleMatch(bool matchOver, bool resolving, bool expected)
    {
        SetState(matchOver, resolving);
        Assert.That(CanRestart(), Is.EqualTo(expected));
    }

    [Test]
    public void DisabledRestartRejectsCompletedMatch()
    {
        SetState(true, false);
        controlsType.GetField("enableEnterToRestart", AnyInstance).SetValue(controls, false);
        Assert.That(CanRestart(), Is.False);
    }

    [Test]
    public void MissingManagerRejectsRestart()
    {
        singleton.SetValue(null, null);
        Assert.That(CanRestart(), Is.False);
    }
}
