using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SoloBotBootstrapTests
{
    Type boardType;
    Type driverType;
    Type settingsType;
    Type bootstrapType;
    GameObject root;

    static Type RuntimeType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing runtime type: " + name);
    }

    static object DefaultArg(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue) return parameter.DefaultValue;
        var type = parameter.ParameterType;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (type == typeof(bool)) return false;
        if (type == typeof(int)) return 1;
        if (type == typeof(string)) return "Pine";
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    // Calls a static method with its defaults, or the first tier for an enum, so the test does not hard-code signatures.
    static object CallStatic(Type type, string method)
    {
        var info = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(info, Is.Not.Null, type.Name + "." + method + " not found");
        var parameters = info.GetParameters();
        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++) args[i] = DefaultArg(parameters[i]);
        return info.Invoke(null, args);
    }

    UnityEngine.Object[] Drivers => UnityEngine.Object.FindObjectsOfType(driverType);

    Component AddBoard()
    {
        var board = root.AddComponent(boardType);
        // Prevent Start from generating tiles.
        ((Behaviour)board).enabled = false;
        return board;
    }

    [SetUp]
    public void SetUp()
    {
        boardType = RuntimeType("BoardManager");
        driverType = RuntimeType("SoloBotDriver");
        settingsType = RuntimeType("SoloSettings");
        bootstrapType = RuntimeType("SoloBotBootstrap");
        CallStatic(settingsType, "Clear");
        Assert.That(Drivers.Length, Is.Zero, "Run this fixture without a live SoloBotDriver.");
        root = new GameObject("Solo bootstrap fixture");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        CallStatic(settingsType, "Clear");
        foreach (var driver in Drivers)
            UnityEngine.Object.Destroy(((Component)driver).gameObject);
        if (root) UnityEngine.Object.Destroy(root);
        yield return null;
    }

    [Test]
    public void SoloWithBoardCreatesOneWiredDriver()
    {
        var board = AddBoard();
        CallStatic(settingsType, "StartSolo");
        CallStatic(bootstrapType, "Ensure");
        var drivers = Drivers;
        Assert.That(drivers.Length, Is.EqualTo(1));
        Assert.That(driverType.GetField("board").GetValue(drivers[0]), Is.SameAs(board));
    }

    [Test]
    public void EnsureTwiceDoesNotDuplicateDriver()
    {
        AddBoard();
        CallStatic(settingsType, "StartSolo");
        CallStatic(bootstrapType, "Ensure");
        CallStatic(bootstrapType, "Ensure");
        Assert.That(Drivers.Length, Is.EqualTo(1));
    }

    [Test]
    public void SoloOffCreatesNoDriver()
    {
        AddBoard();
        Assert.That(CallStatic(bootstrapType, "Ensure"), Is.Null);
        Assert.That(Drivers.Length, Is.Zero);
    }

    [Test]
    public void NoBoardCreatesNoDriver()
    {
        CallStatic(settingsType, "StartSolo");
        Assert.That(CallStatic(bootstrapType, "Ensure"), Is.Null);
        Assert.That(Drivers.Length, Is.Zero);
    }
}
