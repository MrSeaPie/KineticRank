using NUnit.Framework;
using UnityEngine;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    public class PlayerRankProfileTests
    {
        [TestCase(1, RankTier.Bronze, 3)]
        [TestCase(699, RankTier.Bronze, 3)]
        [TestCase(700, RankTier.Bronze, 2)]
        [TestCase(800, RankTier.Bronze, 1)]
        [TestCase(899, RankTier.Bronze, 1)]
        [TestCase(900, RankTier.Silver, 3)]
        [TestCase(1000, RankTier.Silver, 2)]
        [TestCase(1100, RankTier.Silver, 1)]
        [TestCase(1200, RankTier.Gold, 3)]
        [TestCase(1500, RankTier.Platinum, 3)]
        [TestCase(1800, RankTier.Diamond, 3)]
        [TestCase(2099, RankTier.Diamond, 1)]
        [TestCase(2100, RankTier.Master, 0)]
        public void Bands(int rating, RankTier tier, int division)
        {
            Assert.AreEqual(tier, RankBands.TierFor(rating));
            Assert.AreEqual(division, RankBands.DivisionFor(rating, tier));
        }

        [Test]
        public void FivePlacementsUse48ThenUse32()
        {
            var p = new PlayerRankProfile();
            for (int i = 0; i < 5; i++)
            {
                int before = p.rating;
                p.RecordMatch(before, true);
                Assert.AreEqual(24, p.lastChange);
                Assert.AreEqual(4 - i, p.placementGamesLeft);
            }
            Assert.IsFalse(p.IsPlacement);
            p.RecordMatch(p.rating, true);
            Assert.AreEqual(16, p.lastChange);
        }

        [Test]
        public void PromotionProtectsThreeSubsequentGamesThenAllowsDemotion()
        {
            var p = new PlayerRankProfile { rating = 1199, gamesPlayed = 5,
                placementGamesLeft = 0, currentTier = RankTier.Silver, currentDivision = 1 };
            p.RecordMatch(1199, true);
            Assert.AreEqual(RankTier.Gold, p.currentTier);
            Assert.AreEqual(3, p.promotionProtectionGamesLeft);
            for (int i = 0; i < 3; i++)
            {
                p.RecordMatch(1, false);
                Assert.Less(p.rating, 1200);
                Assert.AreEqual(RankTier.Gold, p.currentTier);
                Assert.AreEqual(2 - i, p.promotionProtectionGamesLeft);
            }
            p.RecordMatch(1, false);
            Assert.AreEqual(RankTier.Silver, p.currentTier);
        }

        [Test]
        public void DrawFloorCapAndJsonPreserveContract()
        {
            var p = new PlayerRankProfile();
            p.RecordDraw(1000);
            Assert.AreEqual(1000, p.rating);
            Assert.AreEqual(1, p.gamesPlayed);
            p.rating = 1;
            p.RecordMatch(1, false);
            Assert.AreEqual(1, p.rating);
            p.rating = 2099;
            p.RecordMatch(2099, true);
            Assert.AreEqual(2099, p.rating);
            Assert.AreEqual(RankTier.Diamond, p.currentTier);
            var loaded = JsonUtility.FromJson<PlayerRankProfile>(JsonUtility.ToJson(p));
            Assert.AreEqual(p.rating, loaded.rating);
            Assert.AreEqual(p.peakRating, loaded.peakRating);
            Assert.AreEqual(p.gamesPlayed, loaded.gamesPlayed);
            Assert.AreEqual(p.placementGamesLeft, loaded.placementGamesLeft);
            Assert.AreEqual(p.currentTier, loaded.currentTier);
            Assert.AreEqual(p.currentDivision, loaded.currentDivision);
            Assert.AreEqual(p.season, loaded.season);
        }
    }
}
