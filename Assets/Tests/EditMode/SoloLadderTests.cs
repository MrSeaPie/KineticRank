using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class SoloLadderTests
    {
        [Test]
        public void GetTierStyle_ReturnsExpectedBotStyles()
        {
            Assert.AreEqual(BotStyle.Spender, SoloLadder.GetTierStyle(BotTier.Pine));
            Assert.AreEqual(BotStyle.Positional, SoloLadder.GetTierStyle(BotTier.Birch));
            Assert.AreEqual(BotStyle.Default, SoloLadder.GetTierStyle(BotTier.Oak));
            Assert.AreEqual(BotStyle.Attacker, SoloLadder.GetTierStyle(BotTier.Walnut));
            Assert.AreEqual(BotStyle.Hoarder, SoloLadder.GetTierStyle(BotTier.Ebony));
        }

        [Test]
        public void GetTierDepth_ReturnsMonotonicallyIncreasingDepths()
        {
            Assert.AreEqual(1, SoloLadder.GetTierDepth(BotTier.Pine));
            Assert.AreEqual(2, SoloLadder.GetTierDepth(BotTier.Birch));
            Assert.AreEqual(3, SoloLadder.GetTierDepth(BotTier.Oak));
            Assert.AreEqual(4, SoloLadder.GetTierDepth(BotTier.Walnut));
            Assert.AreEqual(5, SoloLadder.GetTierDepth(BotTier.Ebony));
        }

        [Test]
        public void GetTierRating_ReturnsMonotonicallyIncreasingRatings()
        {
            Assert.AreEqual(800, SoloLadder.GetTierRating(BotTier.Pine));
            Assert.AreEqual(1000, SoloLadder.GetTierRating(BotTier.Birch));
            Assert.AreEqual(1200, SoloLadder.GetTierRating(BotTier.Oak));
            Assert.AreEqual(1400, SoloLadder.GetTierRating(BotTier.Walnut));
            Assert.AreEqual(1600, SoloLadder.GetTierRating(BotTier.Ebony));
        }

        [Test]
        public void GetTierForRating_AssignsExpectedTiers()
        {
            Assert.AreEqual(BotTier.Pine, SoloLadder.GetTierForRating(750));
            Assert.AreEqual(BotTier.Pine, SoloLadder.GetTierForRating(899));
            Assert.AreEqual(BotTier.Birch, SoloLadder.GetTierForRating(900));
            Assert.AreEqual(BotTier.Birch, SoloLadder.GetTierForRating(1099));
            Assert.AreEqual(BotTier.Oak, SoloLadder.GetTierForRating(1100));
            Assert.AreEqual(BotTier.Oak, SoloLadder.GetTierForRating(1299));
            Assert.AreEqual(BotTier.Walnut, SoloLadder.GetTierForRating(1300));
            Assert.AreEqual(BotTier.Walnut, SoloLadder.GetTierForRating(1499));
            Assert.AreEqual(BotTier.Ebony, SoloLadder.GetTierForRating(1500));
            Assert.AreEqual(BotTier.Ebony, SoloLadder.GetTierForRating(1800));
        }

        [Test]
        public void CalculateLadderMatchDelta_WinAgainstHigherTier_AwardsPositiveDelta()
        {
            int delta = SoloLadder.CalculateLadderMatchDelta(1000, BotTier.Oak, playerWon: true);
            Assert.Greater(delta, 16);
        }

        [Test]
        public void CalculateLadderMatchDelta_LossAgainstHigherTier_PenalizesLessThanEqualOpponent()
        {
            int delta = SoloLadder.CalculateLadderMatchDelta(1000, BotTier.Oak, playerWon: false);
            Assert.Greater(delta, -16);
            Assert.Less(delta, 0);
        }

        [Test]
        public void CalculateLadderDrawDelta_EqualRating_YieldsZeroDelta()
        {
            int delta = SoloLadder.CalculateLadderDrawDelta(1000, BotTier.Birch);
            Assert.AreEqual(0, delta);
        }
    }
}
