using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RankedSessionAdmissionTests
{
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

    [TestCase(true)]
    [TestCase(false)]
    public void CreatingSessionDoesNotSettleOrMutateProfile(bool humanIsRed)
    {
        var store = FindType("PlayerRankStore");
        var getProfile = store.GetMethod("GetProfile", BindingFlags.Public | BindingFlags.Static);
        using ((IDisposable)store.GetMethod("BeginIsolatedTestScope").Invoke(null, null))
        {
            string before = JsonUtility.ToJson(getProfile.Invoke(null, null));
            var sessionType = FindType("RankedMatchSession");
            var session = Activator.CreateInstance(sessionType, 1375, humanIsRed);
            Assert.That(sessionType.GetProperty("IsCompleted").GetValue(session), Is.False);
            Assert.That(sessionType.GetProperty("RatingChange").GetValue(session), Is.EqualTo(0));
            Assert.That(JsonUtility.ToJson(getProfile.Invoke(null, null)), Is.EqualTo(before));
        }
    }

    [Test]
    public void NegativeOpponentRatingIsRejectedWithoutMutatingProfile()
    {
        var store = FindType("PlayerRankStore");
        var getProfile = store.GetMethod("GetProfile", BindingFlags.Public | BindingFlags.Static);
        using ((IDisposable)store.GetMethod("BeginIsolatedTestScope").Invoke(null, null))
        {
            string before = JsonUtility.ToJson(getProfile.Invoke(null, null));
            var error = Assert.Throws<TargetInvocationException>(() =>
                Activator.CreateInstance(FindType("RankedMatchSession"), -1, true));
            Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(JsonUtility.ToJson(getProfile.Invoke(null, null)), Is.EqualTo(before));
        }
    }
}
