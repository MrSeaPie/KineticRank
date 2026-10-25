using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RankedMatchPresentationTests
{
    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }

    [Test]
    public void Format_RejectsMissingSessionAndProfile()
    {
        var formatter = RuntimeType("RankedMatchPresentation").GetMethod("Format");
        var error = Assert.Throws<TargetInvocationException>(() => formatter.Invoke(null, new object[] { null, null }));
        Assert.That(error.InnerException, Is.TypeOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)error.InnerException).ParamName, Is.EqualTo("session"));
        var session = Activator.CreateInstance(RuntimeType("RankedMatchSession"), new object[] { 1200, true });
        error = Assert.Throws<TargetInvocationException>(() => formatter.Invoke(null, new object[] { session, null }));
        Assert.That(error.InnerException, Is.TypeOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)error.InnerException).ParamName, Is.EqualTo("profile"));
    }

    [Test]
    public void Format_ActiveMatchShowsProfileWithoutSettlement()
    {
        var formatter = RuntimeType("RankedMatchPresentation").GetMethod("Format");
        var profileType = formatter.GetParameters()[1].ParameterType;
        var profile = Activator.CreateInstance(profileType);
        profileType.GetField("rating").SetValue(profile, 1375);
        var rank = (string)profileType.GetProperty("DisplayRank").GetValue(profile);
        var session = Activator.CreateInstance(RuntimeType("RankedMatchSession"), new object[] { 1500, true });
        var before = JsonUtility.ToJson(profile);
        var text = (string)formatter.Invoke(null, new object[] { session, profile });
        Assert.That(text, Is.EqualTo("Ranked Solo | " + rank + " | Rating 1375\nOpponent rating 1500"));
        Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
        Assert.That(session.GetType().GetProperty("IsCompleted").GetValue(session), Is.False);
    }

    [TestCase(true, true, "You won")]
    [TestCase(true, false, "You lost")]
    [TestCase(false, true, "You lost")]
    [TestCase(false, false, "You won")]
    [TestCase(true, null, "Draw")]
    [TestCase(false, null, "Draw")]
    public void Format_CompletedMatchShowsImmutableHumanResultAndSettlement(bool humanIsRed, bool? redWon, string result)
    {
        var store = RuntimeType("PlayerRankStore");
        var getProfile = store.GetMethod("GetProfile");
        using ((IDisposable)store.GetMethod("BeginIsolatedTestScope").Invoke(null, null))
        {
            var initial = getProfile.Invoke(null, null);
            string rankBefore = (string)initial.GetType().GetProperty("DisplayRank").GetValue(initial);
            var sessionType = RuntimeType("RankedMatchSession");
            var session = Activator.CreateInstance(sessionType, new object[] { 1375, humanIsRed });
            Assert.That(sessionType.GetMethod("TryComplete").Invoke(session, new object[] { redWon }), Is.True);
            var profile = getProfile.Invoke(null, null);
            var persisted = JsonUtility.ToJson(profile);
            int rating = (int)profile.GetType().GetField("rating").GetValue(profile);
            int change = (int)profile.GetType().GetField("lastChange").GetValue(profile);
            string rankAfter = (string)profile.GetType().GetProperty("DisplayRank").GetValue(profile);
            var formatter = RuntimeType("RankedMatchPresentation").GetMethod("Format");
            var text = (string)formatter.Invoke(null, new object[] { session, profile });
            StringAssert.Contains("Ranked Solo | " + rankAfter + " | Rating " + rating, text);
            StringAssert.Contains("Opponent rating 1375 | Rating change " + (change > 0 ? "+" : "") + change, text);
            StringAssert.Contains(result + " | " + rankBefore + " → " + rankAfter, text);
            int protection = (int)profile.GetType().GetField("promotionProtectionGamesLeft").GetValue(profile);
            if (protection > 0) StringAssert.Contains("Protected: " + protection + " games left", text);
            else StringAssert.DoesNotContain("Protected:", text);
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(persisted));
            profile.GetType().GetField("rating").SetValue(profile, 9999);
            Assert.That(formatter.Invoke(null, new object[] { session, profile }), Is.EqualTo(text));
            Assert.That(sessionType.GetMethod("TryComplete").Invoke(session, new object[] { null }), Is.False);
            Assert.That(formatter.Invoke(null, new object[] { session, profile }), Is.EqualTo(text));
            Assert.That(JsonUtility.ToJson(getProfile.Invoke(null, null)), Is.EqualTo(persisted));
        }
    }
}
