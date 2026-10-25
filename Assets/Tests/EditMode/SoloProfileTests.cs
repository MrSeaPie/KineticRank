using System;
using NUnit.Framework;
using UnityEngine;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class SoloProfileTests
    {
        [Test]
        public void DefaultState_HasNoRecordsAndNoTierBeaten()
        {
            var profile = new SoloProfile();

            Assert.IsFalse(profile.TryGetHighestTierBeaten(out _));
            foreach (BotTier tier in Enum.GetValues(typeof(BotTier)))
            {
                Assert.AreEqual(0, profile.GetWins(tier));
                Assert.AreEqual(0, profile.GetLosses(tier));
                Assert.AreEqual(0, profile.GetDraws(tier));
            }
        }

        [Test]
        public void Results_AreCountedPerTierOnly()
        {
            var profile = new SoloProfile();
            profile.RecordWin(BotTier.Oak);
            profile.RecordLoss(BotTier.Oak);
            profile.RecordLoss(BotTier.Oak);
            profile.RecordDraw(BotTier.Birch);

            Assert.AreEqual(1, profile.GetWins(BotTier.Oak));
            Assert.AreEqual(2, profile.GetLosses(BotTier.Oak));
            Assert.AreEqual(0, profile.GetDraws(BotTier.Oak));
            Assert.AreEqual(1, profile.GetDraws(BotTier.Birch));
            Assert.AreEqual(0, profile.GetWins(BotTier.Birch));
            Assert.AreEqual(0, profile.GetWins(BotTier.Pine));
            Assert.AreEqual(0, profile.GetLosses(BotTier.Ebony));
        }

        [Test]
        public void LossesAndDraws_DoNotCountAsTierBeaten()
        {
            var profile = new SoloProfile();
            profile.RecordLoss(BotTier.Ebony);
            profile.RecordDraw(BotTier.Walnut);

            Assert.IsFalse(profile.TryGetHighestTierBeaten(out _));
        }

        [Test]
        public void HighestTierBeaten_ClimbsAndNeverRegresses()
        {
            var profile = new SoloProfile();

            profile.RecordWin(BotTier.Birch);
            Assert.IsTrue(profile.TryGetHighestTierBeaten(out var best));
            Assert.AreEqual(BotTier.Birch, best);

            profile.RecordWin(BotTier.Walnut);
            Assert.IsTrue(profile.TryGetHighestTierBeaten(out best));
            Assert.AreEqual(BotTier.Walnut, best);

            profile.RecordWin(BotTier.Pine);
            Assert.IsTrue(profile.TryGetHighestTierBeaten(out best));
            Assert.AreEqual(BotTier.Walnut, best);
        }

        [Test]
        public void JsonRoundTrip_PreservesRecordsAndHighestTier()
        {
            var profile = new SoloProfile();
            profile.RecordWin(BotTier.Oak);
            profile.RecordLoss(BotTier.Ebony);
            profile.RecordDraw(BotTier.Pine);

            var loaded = JsonUtility.FromJson<SoloProfile>(JsonUtility.ToJson(profile));

            Assert.AreEqual(1, loaded.GetWins(BotTier.Oak));
            Assert.AreEqual(1, loaded.GetLosses(BotTier.Ebony));
            Assert.AreEqual(1, loaded.GetDraws(BotTier.Pine));
            Assert.IsTrue(loaded.TryGetHighestTierBeaten(out var best));
            Assert.AreEqual(BotTier.Oak, best);
        }

        [Test]
        public void CorruptOrUnknownTierData_IsIgnoredWithoutThrowing()
        {
            var loaded = JsonUtility.FromJson<SoloProfile>(
                "{\"records\":[{\"tier\":\"Mahogany\",\"wins\":3}],\"highestTierBeaten\":\"Mahogany\"}");

            Assert.IsFalse(loaded.TryGetHighestTierBeaten(out _));
            Assert.AreEqual(0, loaded.GetWins(BotTier.Pine));

            loaded.RecordWin(BotTier.Pine);
            Assert.AreEqual(1, loaded.GetWins(BotTier.Pine));
            Assert.IsTrue(loaded.TryGetHighestTierBeaten(out var best));
            Assert.AreEqual(BotTier.Pine, best);
        }

        [Test]
        public void Reset_ClearsRecordsAndHighestTier()
        {
            var profile = new SoloProfile();
            profile.RecordWin(BotTier.Ebony);
            profile.RecordLoss(BotTier.Oak);

            profile.Reset();

            Assert.IsFalse(profile.TryGetHighestTierBeaten(out _));
            Assert.AreEqual(0, profile.GetWins(BotTier.Ebony));
            Assert.AreEqual(0, profile.GetLosses(BotTier.Oak));
        }

        [Test]
        public void SoloResults_DoNotAffectRankedProfile()
        {
            var ranked = new RankedProfile();
            var solo = new SoloProfile();
            solo.RecordWin(BotTier.Walnut);
            solo.RecordLoss(BotTier.Walnut);

            Assert.AreEqual(0, ranked.games);
            Assert.AreEqual(1000, ranked.redElo);
            Assert.AreEqual(1000, ranked.blueElo);
        }
    }
}
