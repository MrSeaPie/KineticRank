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
        Assert.That(formatter, Is.Not.Null);
        var missingSession = Assert.Throws<TargetInvocationException>(() => formatter.Invoke(null, new object[] { null, null }));
        Assert.That(missingSession.InnerException, Is.TypeOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)missingSession.InnerException).ParamName, Is.EqualTo("session"));

        var session = Activator.CreateInstance(RuntimeType("RankedMatchSession"), new object[] { 1200, true });
        var missingProfile = Assert.Throws<TargetInvocationException>(() => formatter.Invoke(null, new object[] { session, null }));
        Assert.That(missingProfile.InnerException, Is.TypeOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)missingProfile.InnerException).ParamName, Is.EqualTo("profile"));
    }

    [Test]
    public void Format_ActiveMatchShowsProfileAndCapturedOpponentWithoutSettlement()
    {
        var formatter = RuntimeType("RankedMatchPresentation").GetMethod("Format");
        Assert.That(formatter, Is.Not.Null);
        var profileType = formatter.GetParameters()[1].ParameterType;
        var profile = Activator.CreateInstance(profileType);
        var ratingField = profileType.GetField("rating");
        Assert.That(ratingField, Is.Not.Null);
        ratingField.SetValue(profile, 1375);
        var rankProperty = profileType.GetProperty("DisplayRank");
        Assert.That(rankProperty, Is.Not.Null);
        var rank = (string)rankProperty.GetValue(profile);
        var session = Activator.CreateInstance(RuntimeType("RankedMatchSession"), new object[] { 1500, true });

        var text = (string)formatter.Invoke(null, new object[] { session, profile });

        StringAssert.Contains("Ranked Solo", text);
        StringAssert.Contains(rank, text);
        StringAssert.Contains("Rating 1375", text);
        StringAssert.Contains("Opponent rating 1500", text);
        StringAssert.DoesNotContain("Rating change", text);
        Assert.That(ratingField.GetValue(profile), Is.EqualTo(1375));
        Assert.That(session.GetType().GetProperty("IsCompleted").GetValue(session), Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    [TestCase(null)]
    public void Format_CompletedMatchShowsSettledRatingAndSignedChangeWithoutMutation(bool? redWon)
    {
        var storeType = RuntimeType("PlayerRankStore");
        var getProfile = storeType.GetMethod("GetProfile", BindingFlags.Public | BindingFlags.Static);
        var beginIsolation = storeType.GetMethod("BeginIsolatedTestScope", BindingFlags.Public | BindingFlags.Static);
        Assert.That(getProfile, Is.Not.Null);
        Assert.That(beginIsolation, Is.Not.Null);
        using ((IDisposable)beginIsolation.Invoke(null, null))
        {
            var sessionType = RuntimeType("RankedMatchSession");
            var session = Activator.CreateInstance(sessionType, new object[] { 1375, true });
            Assert.That(sessionType.GetMethod("TryComplete").Invoke(session, new object[] { redWon }), Is.True);
            var profile = getProfile.Invoke(null, null);
            var before = JsonUtility.ToJson(profile);
            int rating = (int)profile.GetType().GetField("rating").GetValue(profile);
            int change = (int)sessionType.GetProperty("RatingChange").GetValue(session);
            string rank = (string)profile.GetType().GetProperty("DisplayRank").GetValue(profile);
            string signedChange = (change > 0 ? "+" : "") + change;
            var formatter = RuntimeType("RankedMatchPresentation").GetMethod("Format");

            var text = (string)formatter.Invoke(null, new object[] { session, profile });

            Assert.That(text, Is.EqualTo("Ranked Solo | " + rank + " | Rating " + rating
                + "\nOpponent rating 1375 | Rating change " + signedChange));
            Assert.That(formatter.Invoke(null, new object[] { session, profile }), Is.EqualTo(text));
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
            Assert.That(JsonUtility.ToJson(getProfile.Invoke(null, null)), Is.EqualTo(before));
            Assert.That(sessionType.GetProperty("IsCompleted").GetValue(session), Is.True);
            Assert.That(sessionType.GetProperty("RatingChange").GetValue(session), Is.EqualTo(change));
        }
    }
}
