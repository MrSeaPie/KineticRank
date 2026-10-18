using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Per-tier record against the bot. Tier is stored by name so reordering BotTier can't remap saved results.
    [Serializable]
    public class SoloTierRecord
    {
        public string tier = "";
        public int wins;
        public int losses;
        public int draws;
    }

    // Solo ladder data, kept apart from the human-vs-human RankedProfile so bot results never touch Elo.
    [Serializable]
    public class SoloProfile
    {
        public List<SoloTierRecord> records = new List<SoloTierRecord>();
        public string highestTierBeaten = "";

        public void RecordWin(BotTier tier)
        {
            GetOrAdd(tier).wins++;
            // Ladder only climbs: a later win on an easier tier keeps the best result
            if (!TryGetHighestTierBeaten(out var best) || BotTiers.Depth(tier) > BotTiers.Depth(best))
                highestTierBeaten = tier.ToString();
        }

        public void RecordLoss(BotTier tier) { GetOrAdd(tier).losses++; }
        public void RecordDraw(BotTier tier) { GetOrAdd(tier).draws++; }

        public int GetWins(BotTier tier) { var r = Find(tier); return r != null ? r.wins : 0; }
        public int GetLosses(BotTier tier) { var r = Find(tier); return r != null ? r.losses : 0; }
        public int GetDraws(BotTier tier) { var r = Find(tier); return r != null ? r.draws : 0; }

        public bool TryGetHighestTierBeaten(out BotTier tier)
        {
            if (!string.IsNullOrEmpty(highestTierBeaten)
                && Enum.TryParse(highestTierBeaten, out tier)
                && Enum.IsDefined(typeof(BotTier), tier))
                return true;
            tier = default;
            return false;
        }

        public void Reset()
        {
            records = new List<SoloTierRecord>();
            highestTierBeaten = "";
        }

        SoloTierRecord Find(BotTier tier)
        {
            if (records == null) return null;
            var name = tier.ToString();
            foreach (var r in records)
                if (r != null && r.tier == name) return r;
            return null;
        }

        SoloTierRecord GetOrAdd(BotTier tier)
        {
            var r = Find(tier);
            if (r != null) return r;
            if (records == null) records = new List<SoloTierRecord>();
            r = new SoloTierRecord { tier = tier.ToString() };
            records.Add(r);
            return r;
        }
    }
}
