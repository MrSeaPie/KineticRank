using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RankedTransitionPresentationTests
{
    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }

    [TestCase(1199, 1, 0, true, true, 5, "Promoted")]
    [TestCase(999, 3, 0, true, true, 5, "Promoted")]
    [TestCase(1000, 2, 0, false, true, 5, "Demoted")]
    [TestCase(900, 3, 0, false, true, 5, "Demoted")]
    [TestCase(900, 3, 2, false, false, 5, "")]
    [TestCase(1000, 2, 0, true, true, 4, "Placement complete")]
    [TestCase(1000, 2, 0, true, true, 3, "")]
    public void Settlement_ShowsRankTransitionAndKeepsSnapshot(int rating, int division, int protection, bool won, bool rankChanges, int gamesPlayed, string transition)
    {
        var store = RuntimeType("PlayerRankStore");
        var getProfile = store.GetMethod("GetProfile");
        using ((IDisposable)store.GetMethod("BeginIsolatedTestScope").Invoke(null, null))
        {
            var profile = getProfile.Invoke(null, null);
            var profileType = profile.GetType();
            profileType.GetField("rating").SetValue(profile, rating);
            profileType.GetField("peakRating").SetValue(profile, rating);
            profileType.GetField("gamesPlayed").SetValue(profile, gamesPlayed);
            profileType.GetField("placementGamesLeft").SetValue(profile, Math.Max(0, 5 - gamesPlayed));
            var tierField = profileType.GetField("currentTier");
            tierField.SetValue(profile, Enum.Parse(tierField.FieldType, "Silver"));
            profileType.GetField("currentDivision").SetValue(profile, division);
            profileType.GetField("promotionProtectionGamesLeft").SetValue(profile, protection);
            store.GetField("data", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, profile);
            var before = (string)profileType.GetProperty("DisplayRank").GetValue(profile);
            var sessionType = RuntimeType("RankedMatchSession");
            var session = Activator.CreateInstance(sessionType, new object[] { rating, true });
            Assert.That(sessionType.GetMethod("TryComplete").Invoke(session, new object[] { won }), Is.True);
            var settled = getProfile.Invoke(null, null);
            var after = (string)profileType.GetProperty("DisplayRank").GetValue(settled);
            Assert.That(after != before, Is.EqualTo(rankChanges));
            if (protection > 0)
                Assert.That(profileType.GetField("promotionProtectionGamesLeft").GetValue(settled), Is.EqualTo(protection - 1));
            var persisted = JsonUtility.ToJson(settled);
            var formatter = RuntimeType("RankedMatchPresentation").GetMethod("Format");
            var text = (string)formatter.Invoke(null, new object[] { session, settled });
            StringAssert.Contains((won ? "You won" : "You lost") + " | " + before + " → " + after, text);
            foreach (var label in new[] { "Promoted", "Demoted", "Placement complete" })
            {
                if (label == transition) StringAssert.Contains(label, text);
                else StringAssert.DoesNotContain(label, text);
            }
            profileType.GetField("rating").SetValue(settled, 9999);
            Assert.That(formatter.Invoke(null, new object[] { session, settled }), Is.EqualTo(text));
            Assert.That(sessionType.GetMethod("TryComplete").Invoke(session, new object[] { null }), Is.False);
            Assert.That(JsonUtility.ToJson(getProfile.Invoke(null, null)), Is.EqualTo(persisted));
        }
    }
}
