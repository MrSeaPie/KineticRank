using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class SoloLadderTests
    {
        [Test]
        public void DepthForTier_DelegatesToBotTiersDepth()
        {
            Assert.AreEqual(1, SoloLadder.DepthForTier(BotTier.Pine));
            Assert.AreEqual(2, SoloLadder.DepthForTier(BotTier.Birch));
            Assert.AreEqual(4, SoloLadder.DepthForTier(BotTier.Oak));
            Assert.AreEqual(6, SoloLadder.DepthForTier(BotTier.Walnut));
            Assert.AreEqual(8, SoloLadder.DepthForTier(BotTier.Ebony));
            Assert.AreEqual(BotTiers.Depth(BotTier.Pine), SoloLadder.DepthForTier(BotTier.Pine));
            Assert.AreEqual(BotTiers.Depth(BotTier.Ebony), SoloLadder.DepthForTier(BotTier.Ebony));
        }

        [Test]
        public void RecommendOpponent_ReturnsAnchorsAtBoundaries()
        {
            Assert.AreEqual(BotTier.Pine, SoloLadder.RecommendOpponent(0));
            Assert.AreEqual(BotTier.Pine, SoloLadder.RecommendOpponent(700));
            Assert.AreEqual(BotTier.Ebony, SoloLadder.RecommendOpponent(2000));
            Assert.AreEqual(BotTier.Pine, SoloLadder.RecommendOpponent(int.MinValue));
            Assert.AreEqual(BotTier.Ebony, SoloLadder.RecommendOpponent(int.MaxValue));
        }

        [Test]
        public void RecommendOpponent_MatchesExactTierRatings()
        {
            Assert.AreEqual(BotTier.Pine, SoloLadder.RecommendOpponent(800));
            Assert.AreEqual(BotTier.Birch, SoloLadder.RecommendOpponent(996));
            Assert.AreEqual(BotTier.Oak, SoloLadder.RecommendOpponent(1255));
            Assert.AreEqual(BotTier.Walnut, SoloLadder.RecommendOpponent(1455));
            Assert.AreEqual(BotTier.Ebony, SoloLadder.RecommendOpponent(1655));
        }

        [Test]
        public void RecommendOpponent_BreaksTieTowardStrongerTier()
        {
            Assert.AreEqual(BotTier.Birch, SoloLadder.RecommendOpponent(898));
        }
    }
}
