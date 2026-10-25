using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KineticRank.Logic;
using UnityEngine;

// Separate move-list history; never changes legacy ratings or player progression.
public sealed class MatchRecordStore
{
    public const int Capacity = 50;
    readonly string path;

    [Serializable]
    sealed class History
    {
        public int FormatVersion = 1;
        public List<MatchRecord> Records = new List<MatchRecord>();
    }

    public MatchRecordStore(string filePath = null)
    {
        path = filePath ?? Path.Combine(Application.persistentDataPath, "kineticrank_matches.json");
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("History path is required.", nameof(filePath));
    }

    // Oldest first. Each read reconstructs independent objects from disk.
    public List<MatchRecord> GetRecords()
    {
        return Load().Records;
    }

    static void Validate(MatchRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        if (string.IsNullOrWhiteSpace(record.Mode))
            throw new InvalidDataException("Match mode is required.");
        DateTime started;
        DateTime ended;
        if (!DateTime.TryParse(record.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out started) ||
            started.Kind != DateTimeKind.Utc ||
            !DateTime.TryParse(record.EndedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out ended) ||
            ended.Kind != DateTimeKind.Utc)
            throw new InvalidDataException("Match timestamps must be valid UTC values.");
        record.Replay();
    }

    public bool Add(MatchRecord record)
    {
        // Validate and snapshot before touching the existing history.
        Validate(record);
        var snapshot = JsonUtility.FromJson<MatchRecord>(JsonUtility.ToJson(record));
        Validate(snapshot);
        var history = Load();
        history.Records.Add(snapshot);
        if (history.Records.Count > Capacity)
            history.Records.RemoveRange(0, history.Records.Count - Capacity);
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(history, true));
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MatchRecordStore] Save failed: " + ex.Message);
            return false;
        }
    }

    History Load()
    {
        if (!File.Exists(path)) return new History();
        try
        {
            var history = JsonUtility.FromJson<History>(File.ReadAllText(path));
            if (history == null || history.FormatVersion != 1 || history.Records == null ||
                history.Records.Count > Capacity)
                throw new InvalidDataException("Invalid match history envelope.");
            foreach (var record in history.Records)
                Validate(record);
            return history;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MatchRecordStore] Load failed: " + ex.Message);
            return new History();
        }
    }
}
