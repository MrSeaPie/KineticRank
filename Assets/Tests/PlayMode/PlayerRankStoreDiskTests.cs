using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerRankStoreDiskTests
{
    Type storeType;
    FieldInfo cache;
    IDisposable isolation;

    object Invoke(string name, params object[] args)
    {
        var method = storeType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(null, args);
    }

    object Profile() => Invoke("GetProfile");
    int Value(object profile, string field) => (int)profile.GetType().GetField(field).GetValue(profile);
    string SavePath => (string)storeType.GetProperty("SavePath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

    [SetUp]
    public void SetUp()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            storeType = assembly.GetType("PlayerRankStore");
            if (storeType != null) break;
        }
        Assert.That(storeType, Is.Not.Null);
        isolation = (IDisposable)Invoke("BeginIsolatedTestScope");
        cache = storeType.GetField("data", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(cache, Is.Not.Null);
        Assert.That(Path.GetFileName(SavePath), Does.StartWith("kineticrank_player_test_"));
    }

    [TearDown]
    public void TearDown()
    {
        isolation?.Dispose();
        isolation = null;
    }

    [Test]
    public void ResultsAndProgressionSurviveReloadAndContinue()
    {
        Invoke("RecordMatch", 1000, true, true);
        Invoke("RecordMatch", 1200, false, true);
        Invoke("RecordDraw", 1100, true);
        object expected = Profile();
        Assert.That(Value(expected, "gamesPlayed"), Is.EqualTo(3));
        Assert.That(Value(expected, "placementGamesLeft"), Is.EqualTo(2));
        string expectedJson = JsonUtility.ToJson(expected);
        Assert.That(File.Exists(SavePath), Is.True);
        cache.SetValue(null, null);
        Assert.That(JsonUtility.ToJson(Profile()), Is.EqualTo(expectedJson));
        Invoke("RecordMatch", 1000, true, true);
        cache.SetValue(null, null);
        Assert.That(Value(Profile(), "gamesPlayed"), Is.EqualTo(4));
        Assert.That(Value(Profile(), "placementGamesLeft"), Is.EqualTo(1));
    }

    [Test]
    public void ReturnedProfileCannotMutateCachedOrPersistedProgression()
    {
        Invoke("RecordMatch", 1000, true, true);
        object snapshot = Profile();
        string expected = JsonUtility.ToJson(snapshot);
        snapshot.GetType().GetField("rating").SetValue(snapshot, 42);
        snapshot.GetType().GetField("gamesPlayed").SetValue(snapshot, 99);
        Assert.That(JsonUtility.ToJson(Profile()), Is.EqualTo(expected));
        cache.SetValue(null, null);
        Assert.That(JsonUtility.ToJson(Profile()), Is.EqualTo(expected));
    }

    [TestCase("{ definitely invalid JSON")]
    [TestCase("{\"rating\":-1,\"gamesPlayed\":-1}")]
    public void InvalidSaveRecoversAndNextResultPersists(string invalidJson)
    {
        string initial = JsonUtility.ToJson(Profile());
        Invoke("RecordMatch", 1000, true, true);
        File.WriteAllText(SavePath, invalidJson);
        cache.SetValue(null, null);
        LogAssert.Expect(LogType.Warning, new Regex(@"\[PlayerRankStore\] Load failed:"));
        Assert.That(JsonUtility.ToJson(Profile()), Is.EqualTo(initial));
        Invoke("RecordDraw", 1000, true);
        string recovered = JsonUtility.ToJson(Profile());
        cache.SetValue(null, null);
        Assert.That(JsonUtility.ToJson(Profile()), Is.EqualTo(recovered));
        Assert.That(Value(Profile(), "gamesPlayed"), Is.EqualTo(1));
    }
}
