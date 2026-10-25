using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MatchRecordStoreTests
{
    string directory;
    string path;
    Type storeType;
    Type recordType;

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
        storeType = FindType("MatchRecordStore");
        recordType = FindType("KineticRank.Logic.MatchRecord");
        directory = Path.Combine(Path.GetTempPath(), "KineticRankHistory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "matches.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    object Store(string destination = null) => Activator.CreateInstance(storeType, new object[] { destination ?? path });
    IList Records() => (IList)storeType.GetMethod("GetRecords").Invoke(Store(), null);
    bool Add(object record, string destination = null)
    {
        try { return (bool)storeType.GetMethod("Add").Invoke(Store(destination), new[] { record }); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    void Set(object value, string name, object data) => value.GetType().GetField(name).SetValue(value, data);

    object Record(int index)
    {
        var record = Activator.CreateInstance(recordType);
        Set(record, "Mode", "Solo");
        Set(record, "RedLabel", "Player " + index);
        Set(record, "BlueLabel", "Pine");
        Set(record, "RedRating", 1000 + index);
        Set(record, "BlueRating", 800);
        Set(record, "RedStarts", false);
        var reason = recordType.GetField("EndReason");
        reason.SetValue(record, Enum.Parse(reason.FieldType, "Abandoned"));
        var result = recordType.GetField("Result");
        result.SetValue(record, Enum.Parse(result.FieldType, "InProgress"));
        Set(record, "StartedUtc", "2026-10-20T20:00:00.0000000Z");
        Set(record, "EndedUtc", "2026-10-20T20:01:00.0000000Z");
        Set(record, "Plies", 1);
        var moveType = FindType("KineticRank.Logic.RecordedMove");
        var move = JsonUtility.FromJson("{\"Column\":3,\"Kind\":0}", moveType);
        ((IList)Field(record, "Moves")).Add(move);
        return record;
    }

    [Test]
    public void MissingFile_ReturnsEmptyWithoutCreatingFile()
    {
        Assert.IsEmpty(Records());
        Assert.IsFalse(File.Exists(path));
    }

    [Test]
    public void Add_ReloadsMetadataMovesAndIndependentSnapshots()
    {
        var original = Record(7);
        var expected = JsonUtility.ToJson(original);
        Assert.IsTrue(Add(original));
        Set(original, "RedLabel", "Changed");
        ((IList)Field(original, "Moves")).Clear();
        var records = Records();
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(expected, JsonUtility.ToJson(records[0]));
        Assert.AreEqual("N4", recordType.GetMethod("CompactNotation").Invoke(records[0], null));
        ((IList)Field(records[0], "Moves")).Clear();
        Assert.AreEqual(expected, JsonUtility.ToJson(Records()[0]));
    }

    [Test]
    public void Add_RetainsNewestFiftyInInsertionOrderAcrossReload()
    {
        for (int i = 0; i < 53; i++) Assert.IsTrue(Add(Record(i)));
        var records = Records();
        Assert.AreEqual(50, records.Count);
        for (int i = 0; i < records.Count; i++)
            Assert.AreEqual("Player " + (i + 3), Field(records[i], "RedLabel"));
    }

    [Test]
    public void Add_InvalidRecordLeavesExistingFileUnchanged()
    {
        Assert.IsTrue(Add(Record(0)));
        var before = File.ReadAllText(path);
        var invalid = Record(1);
        Set(invalid, "Plies", 2);
        Assert.Throws<InvalidOperationException>(() => Add(invalid));
        Assert.Throws<ArgumentNullException>(() => Add(null));
        Assert.AreEqual(before, File.ReadAllText(path));
    }

    [TestCase("{broken")]
    [TestCase("{\"FormatVersion\":99,\"Records\":[]}")]
    [TestCase("{\"FormatVersion\":1,\"Records\":[null]}")]
    public void Load_CorruptOrUnsupportedHistoryReturnsEmptyWithoutRewriting(string json)
    {
        File.WriteAllText(path, json);
        LogAssert.Expect(LogType.Warning, new Regex("\\[MatchRecordStore\\] Load failed:"));
        Assert.IsEmpty(Records());
        Assert.AreEqual(json, File.ReadAllText(path));
    }

    [Test]
    public void Add_UnwritableDestinationReportsFailure()
    {
        LogAssert.Expect(LogType.Warning, new Regex("\\[MatchRecordStore\\] Save failed:"));
        Assert.IsFalse(Add(Record(0), directory));
        Assert.IsTrue(Directory.Exists(directory));
    }
}
