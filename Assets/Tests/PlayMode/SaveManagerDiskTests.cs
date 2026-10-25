using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveManagerDiskTests
{
    Type saveType;
    FieldInfo cachedData;
    IDisposable isolation;

    object Invoke(string method, params object[] arguments)
    {
        var target = saveType.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        Assert.That(target, Is.Not.Null, "Missing SaveManager API: " + method);
        return target.Invoke(null, arguments);
    }

    [SetUp]
    public void SetUp()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            saveType = assembly.GetType("SaveManager");
            if (saveType != null) break;
        }
        Assert.That(saveType, Is.Not.Null);
        isolation = (IDisposable)Invoke("BeginIsolatedTestScope");
        cachedData = saveType.GetField("data", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(cachedData, Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        isolation?.Dispose();
        isolation = null;
    }

    [Test]
    public void RecordedResultsSurviveDiskReload()
    {
        Invoke("RecordMatch", true);
        Invoke("RecordMatch", false);
        Invoke("RecordDraw");
        string[] getters = {
            "GetGames", "GetRedWins", "GetBlueWins", "GetDraws",
            "GetRedElo", "GetBlueElo"
        };
        var expected = new int[getters.Length];
        for (int i = 0; i < getters.Length; i++)
            expected[i] = (int)Invoke(getters[i]);
        Assert.That(expected[0], Is.EqualTo(3));
        Assert.That(expected[1], Is.EqualTo(1));
        Assert.That(expected[2], Is.EqualTo(1));
        Assert.That(expected[3], Is.EqualTo(1));

        // Discard only the isolated cache so the next getter must load its file.
        cachedData.SetValue(null, null);
        for (int i = 0; i < getters.Length; i++)
            Assert.That((int)Invoke(getters[i]), Is.EqualTo(expected[i]), getters[i]);

        Invoke("RecordMatch", true);
        cachedData.SetValue(null, null);
        Assert.That((int)Invoke("GetGames"), Is.EqualTo(4));
        Assert.That((int)Invoke("GetRedWins"), Is.EqualTo(2));
        Assert.That((int)Invoke("GetBlueWins"), Is.EqualTo(1));
        Assert.That((int)Invoke("GetDraws"), Is.EqualTo(1));
    }

    [Test]
    public void MalformedSaveFallsBackAndSubsequentResultPersists()
    {
        int initialRedElo = (int)Invoke("GetRedElo");
        int initialBlueElo = (int)Invoke("GetBlueElo");
        var pathProperty = saveType.GetProperty("SavePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(pathProperty, Is.Not.Null);
        string isolatedPath = (string)pathProperty.GetValue(null);
        Assert.That(Path.GetFileName(isolatedPath), Does.StartWith("kineticrank_test_"));
        Invoke("RecordMatch", true);
        File.WriteAllText(isolatedPath, "{ definitely invalid JSON");
        cachedData.SetValue(null, null);
        LogAssert.Expect(LogType.Warning, new Regex(@"\[SaveManager\] Load failed:"));
        Assert.That((int)Invoke("GetGames"), Is.EqualTo(0));
        Assert.That((int)Invoke("GetRedWins"), Is.EqualTo(0));
        Assert.That((int)Invoke("GetBlueWins"), Is.EqualTo(0));
        Assert.That((int)Invoke("GetDraws"), Is.EqualTo(0));
        Assert.That((int)Invoke("GetRedElo"), Is.EqualTo(initialRedElo));
        Assert.That((int)Invoke("GetBlueElo"), Is.EqualTo(initialBlueElo));

        Invoke("RecordMatch", false);
        int savedBlueElo = (int)Invoke("GetBlueElo");
        cachedData.SetValue(null, null);
        Assert.That((int)Invoke("GetGames"), Is.EqualTo(1));
        Assert.That((int)Invoke("GetBlueWins"), Is.EqualTo(1));
        Assert.That((int)Invoke("GetRedWins"), Is.EqualTo(0));
        Assert.That((int)Invoke("GetBlueElo"), Is.EqualTo(savedBlueElo));
    }
}
