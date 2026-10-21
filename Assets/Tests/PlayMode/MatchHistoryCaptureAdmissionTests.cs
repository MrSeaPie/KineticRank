using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class MatchHistoryCaptureAdmissionTests
{
    static Type CaptureType()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("MatchHistoryCapture", false))
            .FirstOrDefault(t => t != null);
        Assert.That(type, Is.Not.Null, "Runtime capture type must be available.");
        return type;
    }

    static object Create(Type type)
    {
        return Activator.CreateInstance(type, new object[]
        {
            "test", true, true, "Red", "Blue", 1000, 1000, null
        });
    }

    static bool Move(Type type, object capture, int column, bool red)
    {
        var method = type.GetMethod("RecordMove");
        var kind = Enum.Parse(method.GetParameters()[1].ParameterType, "Normal");
        return (bool)method.Invoke(capture, new object[] { column, kind, red });
    }

    static bool Complete(Type type, object capture)
    {
        var method = type.GetMethod("Complete");
        var parameters = method.GetParameters();
        return (bool)method.Invoke(capture, new object[]
        {
            Enum.ToObject(parameters[0].ParameterType, 0),
            Enum.Parse(parameters[1].ParameterType, "Timeout")
        });
    }

    [Test]
    public void WrongMoverDiscardsSessionAndRejectsLaterLegalMoves()
    {
        var type = CaptureType();
        var capture = Create(type);
        Assert.That(Move(type, capture, 0, false), Is.False);
        Assert.That(Move(type, capture, 0, true), Is.False);
        Assert.That(Complete(type, capture), Is.False);
        Assert.That(Complete(type, capture), Is.False);
    }

    [Test]
    public void OutOfRangeMoveDiscardsSessionAndCompletionClosesAdmission()
    {
        var type = CaptureType();
        var capture = Create(type);
        Assert.That(Move(type, capture, -1, true), Is.False);
        Assert.That(Complete(type, capture), Is.False);
        Assert.That(Move(type, capture, 0, true), Is.False);
    }
}
