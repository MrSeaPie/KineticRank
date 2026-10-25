using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RankedAdmissionTests
{
    GameObject root;
    Type managerType;
    Type soloType;

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
        if (match != null) return match;
        throw new InvalidOperationException("Missing runtime type: " + name);
    }

    static void ClearSettings(string name)
    {
        RuntimeType(name).GetMethod("Clear").Invoke(null, null);
    }

    [SetUp]
    public void SetUp()
    {
        managerType = RuntimeType("GameManager");
        soloType = RuntimeType("SoloSettings");
        Assert.That(UnityEngine.Object.FindObjectsOfType(managerType).Length, Is.Zero,
            "Admission tests require an isolated scene.");
        ClearSettings("SoloSettings");
        ClearSettings("PuzzleSettings");
        ClearSettings("TraditionalSettings");
    }

    [TearDown]
    public void TearDown()
    {
        if (root) UnityEngine.Object.DestroyImmediate(root);
        ClearSettings("SoloSettings");
        ClearSettings("PuzzleSettings");
        ClearSettings("TraditionalSettings");
    }

    void ConfigureSolo(string method)
    {
        var start = soloType.GetMethod(method);
        var tier = Enum.Parse(start.GetParameters()[0].ParameterType, "Pine");
        start.Invoke(null, new object[] { tier, false });
    }

    Component Admit()
    {
        root = new GameObject("Ranked admission fixture");
        root.SetActive(false);
        var manager = root.AddComponent(managerType);
        ((Behaviour)manager).enabled = false;
        root.SetActive(true);
        return manager;
    }

    object Session(Component manager)
    {
        return managerType.GetField("rankedSession", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(manager);
    }

    [Test]
    public void RankedSoloCapturesSessionAndRetainsItWhenMenuSettingsClear()
    {
        ConfigureSolo("StartRankedSolo");
        var manager = Admit();
        var admitted = Session(manager);
        Assert.That(admitted, Is.Not.Null);
        ClearSettings("SoloSettings");
        Assert.That(Session(manager), Is.SameAs(admitted));
    }

    [Test]
    public void CasualSoloDoesNotAdmitRankedSession()
    {
        ConfigureSolo("StartSolo");
        Assert.That(Session(Admit()), Is.Null);
    }

    [Test]
    public void HotSeatDoesNotAdmitRankedSession()
    {
        Assert.That(Session(Admit()), Is.Null);
    }
}
