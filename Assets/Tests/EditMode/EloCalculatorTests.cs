using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class EloCalculatorTests
    {
        [Test]
        public void ExpectedScore_EqualRatings_ReturnsHalf()
        {
            float expRed = EloCalculator.ExpectedScore(1000, 1000);
            float expBlue = EloCalculator.ExpectedScore(1000, 1000);

            Assert.AreEqual(0.5f, expRed, 0.0001f);
            Assert.AreEqual(0.5f, expBlue, 0.0001f);
            Assert.AreEqual(1f, expRed + expBlue, 0.0001f);
        }

        [Test]
        public void ExpectedScore_HigherRatedPlayer_HasHigherProbability()
        {
            float expHigher = EloCalculator.ExpectedScore(1400, 1000);
            float expLower = EloCalculator.ExpectedScore(1000, 1400);

            Assert.Greater(expHigher, 0.5f);
            Assert.Less(expLower, 0.5f);
            Assert.AreEqual(1f, expHigher + expLower, 0.0001f);
        }

        [Test]
        public void CalculateMatch_RedWinEqualRatings_AppliesExpectedDeltas()
        {
            EloCalculator.CalculateMatch(
                redElo: 1000,
                blueElo: 1000,
                redWon: true,
                newRed: out int newRed,
                newBlue: out int newBlue,
                deltaRed: out int deltaRed,
                deltaBlue: out int deltaBlue,
                kFactor: 32
            );

            Assert.AreEqual(16, deltaRed);
            Assert.AreEqual(-16, deltaBlue);
            Assert.AreEqual(1016, newRed);
            Assert.AreEqual(984, newBlue);
        }

        [Test]
        public void CalculateMatch_BlueWinEqualRatings_AppliesExpectedDeltas()
        {
            EloCalculator.CalculateMatch(
                redElo: 1000,
                blueElo: 1000,
                redWon: false,
                newRed: out int newRed,
                newBlue: out int newBlue,
                deltaRed: out int deltaRed,
                deltaBlue: out int deltaBlue,
                kFactor: 32
            );

            Assert.AreEqual(-16, deltaRed);
            Assert.AreEqual(16, deltaBlue);
            Assert.AreEqual(984, newRed);
            Assert.AreEqual(1016, newBlue);
        }

        [Test]
        public void CalculateMatch_UnderdogWin_YieldsLargerDeltaThanFavoriteWin()
        {
            EloCalculator.CalculateMatch(1400, 1000, redWon: true, out _, out _, out int favDeltaRed, out _);
            EloCalculator.CalculateMatch(1400, 1000, redWon: false, out _, out _, out _, out int dogDeltaBlue);

            Assert.Greater(dogDeltaBlue, favDeltaRed);
        }

        [Test]
        public void CalculateMatch_RatingFloor_ClampsAtMinimumOne()
        {
            EloCalculator.CalculateMatch(
                redElo: 10,
                blueElo: 2000,
                redWon: false,
                newRed: out int newRed,
                newBlue: out _,
                deltaRed: out int deltaRed,
                deltaBlue: out _
            );

            Assert.Less(10 + deltaRed, 1);
            Assert.AreEqual(EloCalculator.MinRating, newRed);
        }

        [Test]
        public void CalculateMatch_CustomKFactor_ScalesRatingChange()
        {
            EloCalculator.CalculateMatch(
                redElo: 1000,
                blueElo: 1000,
                redWon: true,
                newRed: out int newRed,
                newBlue: out int newBlue,
                deltaRed: out int deltaRed,
                deltaBlue: out int deltaBlue,
                kFactor: 64
            );

            Assert.AreEqual(32, deltaRed);
            Assert.AreEqual(-32, deltaBlue);
            Assert.AreEqual(1032, newRed);
            Assert.AreEqual(968, newBlue);
        }
    }
}
