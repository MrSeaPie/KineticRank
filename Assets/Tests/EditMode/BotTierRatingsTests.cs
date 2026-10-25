using System;
using System.Globalization;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    public class BotTierRatingsTests
    {
        [Test]
        public void Pine_IsTheAnchor()
        {
            Assert.AreEqual(BotTierRatings.Anchor, BotTierRatings.RatingFor(BotTier.Pine));
        }

        [Test]
        public void EveryTier_HasARating_NeverBelowTheWeakerTier()
        {
            var tiers = (BotTier[])Enum.GetValues(typeof(BotTier));
            for (int i = 1; i < tiers.Length; i++)
            {
                int gap = BotTierRatings.RatingFor(tiers[i]) - BotTierRatings.RatingFor(tiers[i - 1]);
                Assert.That(gap, Is.InRange(0, BotTierRatings.MaxGap), $"{tiers[i - 1]} -> {tiers[i]}");
            }
        }

        [Test]
        public void RatingFor_UnknownTier_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BotTierRatings.RatingFor((BotTier)99));
        }

        [Test]
        public void TiersByRating_ListsEveryTierOnce_Ascending()
        {
            var order = BotTierRatings.TiersByRating;
            Assert.AreEqual(Enum.GetValues(typeof(BotTier)).Length, order.Count);
            CollectionAssert.AllItemsAreUnique(order);
            for (int i = 1; i < order.Count; i++)
            {
                Assert.LessOrEqual(BotTierRatings.RatingFor(order[i - 1]), BotTierRatings.RatingFor(order[i]));
            }
        }

        [TestCase(0.0, 0)]
        [TestCase(0.4, 0)]
        [TestCase(0.5, 0)]
        [TestCase(0.75, 191)]
        [TestCase(0.9, 382)]
        [TestCase(0.95, 400)]
        [TestCase(1.0, 400)]
        public void ScoreToGap_UsesTheEloFormula_Capped(double score, int expected)
        {
            Assert.AreEqual(expected, BotTierRatings.ScoreToGap(score));
        }

        [TestCase(-0.01)]
        [TestCase(1.01)]
        [TestCase(double.NaN)]
        public void ScoreToGap_OutOfRange_Throws(double score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BotTierRatings.ScoreToGap(score));
        }

        [Test]
        public void RatingsFromGaps_AccumulatesFromTheAnchor()
        {
            CollectionAssert.AreEqual(new[] { 800, 991, 1191, 1591, 1591 }, BotTierRatings.RatingsFromGaps(new[] { 191, 200, 400, 0 }));
        }

        [Test]
        public void RatingsFromGaps_RejectsBadInput()
        {
            Assert.Throws<ArgumentException>(() => BotTierRatings.RatingsFromGaps(new[] { 100, 100 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => BotTierRatings.RatingsFromGaps(new[] { 100, 401, 100, 100 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => BotTierRatings.RatingsFromGaps(new[] { 100, -1, 100, 100 }));
        }

        // Calibration runs: adjacent tiers on all 49 two-ply openings, each tier taking Red (first move) once per opening.
        // Deterministic, so every run gives the same numbers. The result line is reported through Assert.Pass.
        const int CalibrationMaxPlies = 120;

        [Test, Explicit("Long calibration run for Docs/RANKED_RATINGS.md")]
        public void Calibrate_LowerTiers()
        {
            Assert.Pass(Pair(BotTier.Pine, BotTier.Birch) + "\n" + Pair(BotTier.Birch, BotTier.Oak));
        }

        [Test, Explicit("Long calibration run for Docs/RANKED_RATINGS.md")]
        public void Calibrate_OakWalnut()
        {
            Assert.Pass(Pair(BotTier.Oak, BotTier.Walnut));
        }

        [Test, Explicit("Long calibration run for Docs/RANKED_RATINGS.md")]
        public void Calibrate_WalnutEbony()
        {
            Assert.Pass(Pair(BotTier.Walnut, BotTier.Ebony));
        }

        static string Pair(BotTier weaker, BotTier stronger)
        {
            var openings = SelfPlay.TwoPlyOpenings();
            int weakDepth = BotTiers.Depth(weaker);
            int strongDepth = BotTiers.Depth(stronger);
            var strongAsRed = SelfPlay.Run(openings, strongDepth, weakDepth, CalibrationMaxPlies);
            var weakAsRed = SelfPlay.Run(openings, weakDepth, strongDepth, CalibrationMaxPlies);

            int games = strongAsRed.GameCount + weakAsRed.GameCount;
            int wins = strongAsRed.RedWins + weakAsRed.BlueWins;
            int losses = strongAsRed.BlueWins + weakAsRed.RedWins;
            int draws = strongAsRed.Draws + weakAsRed.Draws;
            int capped = strongAsRed.CappedGames + weakAsRed.CappedGames;
            double score = (wins + 0.5 * draws) / games;
            return string.Format(CultureInfo.InvariantCulture,
                "{0} vs {1}: {2} games, {1} wins {3}, losses {4}, draws {5} ({6} capped), score {7:F4}, gap {8}",
                weaker, stronger, games, wins, losses, draws, capped, score, BotTierRatings.ScoreToGap(score));
        }
    }
}
