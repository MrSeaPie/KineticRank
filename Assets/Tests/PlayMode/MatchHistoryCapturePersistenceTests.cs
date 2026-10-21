using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class MatchHistoryCapturePersistenceTests
{
    string directory;
    string path;

    static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing type: " + name);
    }

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "KineticRankCapture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "matches.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void Complete_PersistsAdmittedMoveAndRejectsFurtherWrites()
    {
        var storeType = FindType("MatchRecordStore");
        var store = Activator.CreateInstance(storeType, new object[] { path });
        var captureType = FindType("MatchHistoryCapture");
        var capture = Activator.CreateInstance(captureType, new object[]
        {
            "Solo", false, true, "Player", "Pine", 1000, 800, store
        });
        var moveMethod = captureType.GetMethod("RecordMove");
        var normal = Enum.Parse(moveMethod.GetParameters()[1].ParameterType, "Normal");
        Assert.IsTrue((bool)moveMethod.Invoke(capture, new object[] { 3, normal, false }));
        Assert.IsFalse(File.Exists(path), "An unfinished session must not write history.");

        var complete = captureType.GetMethod("Complete");
        var result = Enum.Parse(complete.GetParameters()[0].ParameterType, "InProgress");
        var reason = Enum.Parse(complete.GetParameters()[1].ParameterType, "Abandoned");
        Assert.IsTrue((bool)complete.Invoke(capture, new[] { result, reason }));
        var saved = File.ReadAllText(path);
        Assert.IsFalse((bool)complete.Invoke(capture, new[] { result, reason }));
        Assert.IsFalse((bool)moveMethod.Invoke(capture, new object[] { 4, normal, true }));
        Assert.AreEqual(saved, File.ReadAllText(path), "Completion and moves after completion must leave persisted history unchanged.");

        var reloaded = Activator.CreateInstance(storeType, new object[] { path });
        var records = (IList)storeType.GetMethod("GetRecords").Invoke(reloaded, null);
        Assert.AreEqual(1, records.Count);
        var record = records[0];
        var recordType = record.GetType();
        Assert.AreEqual("N4", recordType.GetMethod("CompactNotation").Invoke(record, null));
        Assert.AreEqual(1, recordType.GetField("Plies").GetValue(record));
        Assert.AreEqual(false, recordType.GetField("RedStarts").GetValue(record));
        Assert.AreEqual("Solo", recordType.GetField("Mode").GetValue(record));
        Assert.AreEqual("Player", recordType.GetField("RedLabel").GetValue(record));
        Assert.AreEqual("Pine", recordType.GetField("BlueLabel").GetValue(record));
        Assert.AreEqual(1000, recordType.GetField("RedRating").GetValue(record));
        Assert.AreEqual(800, recordType.GetField("BlueRating").GetValue(record));
        Assert.AreEqual(result, recordType.GetField("Result").GetValue(record));
        Assert.AreEqual(reason, recordType.GetField("EndReason").GetValue(record));
    }
}
