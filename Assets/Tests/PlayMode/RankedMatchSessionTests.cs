using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RankedMatchSessionTests
{
    Type storeType;
    Type sessionType;
    IDisposable isolation;

    static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        Assert.Fail("Missing runtime type: " + name);
        return null;
    }

    object Store(string method, params object[] args)
    {
        return storeType.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    }

    [SetUp]
    public void SetUp()
    {
        storeType = FindType("PlayerRankStore");
        sessionType = FindType("RankedMatchSession");
        isolation = (IDisposable)Store("BeginIsolatedTestScope");
    }

    [TearDown]
    public void TearDown()
    {
        isolation?.Dispose();
        isolation = null;
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    [TestCase(true, null)]
    [TestCase(false, null)]
    public void CompletionUsesCapturedOpponentAndHumanSideExactlyOnce(bool humanIsRed, bool? redWon)
    {
        const int opponentRating = 1375;
        double score = !redWon.HasValue ? 0.5 : redWon.Value == humanIsRed ? 1.0 : 0.0;
        string expected;
        using ((IDisposable)Store("BeginIsolatedTestScope"))
        {
            Store("RecordResult", opponentRating, score, true);
            expected = JsonUtility.ToJson(Store("GetProfile"));
        }

        var session = Activator.CreateInstance(sessionType, opponentRating, humanIsRed);
        Assert.That(sessionType.GetProperty("OpponentRating").GetValue(session), Is.EqualTo(opponentRating));
        Assert.That(sessionType.GetProperty("HumanIsRed").GetValue(session), Is.EqualTo(humanIsRed));
        Assert.That(sessionType.GetProperty("IsCompleted").GetValue(session), Is.False);
        var complete = sessionType.GetMethod("TryComplete");
        Assert.That(complete.Invoke(session, new object[] { redWon }), Is.True);
        Assert.That(sessionType.GetProperty("IsCompleted").GetValue(session), Is.True);
        object profile = Store("GetProfile");
        Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(expected));
        Assert.That(sessionType.GetProperty("RatingChange").GetValue(session),
            Is.EqualTo(profile.GetType().GetField("lastChange").GetValue(profile)));

        Assert.That(complete.Invoke(session, new object[] { !humanIsRed }), Is.False);
        Assert.That(complete.Invoke(session, new object[] { null }), Is.False);
        Assert.That(JsonUtility.ToJson(Store("GetProfile")), Is.EqualTo(expected));
    }
}
