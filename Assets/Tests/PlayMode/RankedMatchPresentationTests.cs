using System;
using System.Reflection;
using NUnit.Framework;

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
}
