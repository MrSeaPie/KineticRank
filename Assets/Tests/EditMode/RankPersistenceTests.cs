using NUnit.Framework;
using UnityEngine;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class RankPersistenceTests
    {
        [Test]
        public void RankedProfile_DefaultState_MatchesInitialContract()
        {
            var profile = new RankedProfile();

            Assert.AreEqual(1000, profile.redElo);
            Assert.AreEqual(1000, profile.blueElo);
            Assert.AreEqual(0, profile.games);
            Assert.AreEqual(0, profile.lastDeltaRed);
            Assert.AreEqual(0, profile.lastDeltaBlue);
            Assert.AreEqual(0, profile.redWins);
            Assert.AreEqual(0, profile.blueWins);
            Assert.AreEqual(0, profile.draws);
            Assert.IsTrue(profile.hudVisible);
        }

        [Test]
        public void RecordMatch_RedVictory_UpdatesStatsAndRatingsCorrectly()
        {
            var profile = new RankedProfile();
            profile.RecordMatch(redWon: true, kFactor: 32);

            Assert.AreEqual(1, profile.games);
            Assert.AreEqual(1, profile.redWins);
            Assert.AreEqual(0, profile.blueWins);
            Assert.AreEqual(0, profile.draws);
            Assert.AreEqual(16, profile.lastDeltaRed);
            Assert.AreEqual(-16, profile.lastDeltaBlue);
            Assert.AreEqual(1016, profile.redElo);
            Assert.AreEqual(984, profile.blueElo);
        }

        [Test]
        public void RecordMatch_BlueVictory_UpdatesStatsAndRatingsCorrectly()
        {
            var profile = new RankedProfile();
            profile.RecordMatch(redWon: false, kFactor: 32);

            Assert.AreEqual(1, profile.games);
            Assert.AreEqual(0, profile.redWins);
            Assert.AreEqual(1, profile.blueWins);
            Assert.AreEqual(0, profile.draws);
            Assert.AreEqual(-16, profile.lastDeltaRed);
            Assert.AreEqual(16, profile.lastDeltaBlue);
            Assert.AreEqual(984, profile.redElo);
            Assert.AreEqual(1016, profile.blueElo);
        }

        [Test]
        public void RecordDraw_EqualRatings_UpdatesStatsAndDrawCount()
        {
            var profile = new RankedProfile();
            profile.RecordDraw(kFactor: 32);

            Assert.AreEqual(1, profile.games);
            Assert.AreEqual(0, profile.redWins);
            Assert.AreEqual(0, profile.blueWins);
            Assert.AreEqual(1, profile.draws);
            Assert.AreEqual(0, profile.lastDeltaRed);
            Assert.AreEqual(0, profile.lastDeltaBlue);
            Assert.AreEqual(1000, profile.redElo);
            Assert.AreEqual(1000, profile.blueElo);
        }

        [Test]
        public void RecordMatch_RatingFloor_ClampsRatingAndAlignsDelta()
        {
            var profile = new RankedProfile { redElo = 10, blueElo = 10 };
            profile.RecordMatch(redWon: false, kFactor: 32);

            Assert.AreEqual(EloCalculator.MinRating, profile.redElo);
            Assert.GreaterOrEqual(profile.redElo, 1);
            Assert.AreEqual(EloCalculator.MinRating - 10, profile.lastDeltaRed);

            var blueProfile = new RankedProfile { redElo = 10, blueElo = 10 };
            blueProfile.RecordMatch(redWon: true, kFactor: 32);

            Assert.AreEqual(EloCalculator.MinRating, blueProfile.blueElo);
            Assert.GreaterOrEqual(blueProfile.blueElo, 1);
            Assert.AreEqual(EloCalculator.MinRating - 10, blueProfile.lastDeltaBlue);
        }

        [Test]
        public void ResetRanked_DefaultPreservesHudVisibilityOption()
        {
            var profile = new RankedProfile
            {
                redElo = 1250,
                blueElo = 950,
                games = 12,
                redWins = 8,
                blueWins = 3,
                draws = 1,
                hudVisible = false
            };

            profile.ResetRanked(preserveHud: true);

            Assert.AreEqual(1000, profile.redElo);
            Assert.AreEqual(1000, profile.blueElo);
            Assert.AreEqual(0, profile.games);
            Assert.AreEqual(0, profile.redWins);
            Assert.AreEqual(0, profile.blueWins);
            Assert.AreEqual(0, profile.draws);
            Assert.AreEqual(0, profile.lastDeltaRed);
            Assert.AreEqual(0, profile.lastDeltaBlue);
            Assert.IsFalse(profile.hudVisible);
        }

        [Test]
        public void ResetRanked_WithoutPreserveHud_ResetsHudVisibilityToDefault()
        {
            var profile = new RankedProfile
            {
                redElo = 1300,
                blueElo = 900,
                games = 5,
                draws = 1,
                hudVisible = false
            };

            profile.ResetRanked(preserveHud: false);

            Assert.AreEqual(1000, profile.redElo);
            Assert.AreEqual(1000, profile.blueElo);
            Assert.AreEqual(0, profile.draws);
            Assert.IsTrue(profile.hudVisible);
        }

        [Test]
        public void JsonSerialization_RoundTrip_MaintainsStateIntegrity()
        {
            var original = new RankedProfile
            {
                redElo = 1184,
                blueElo = 916,
                games = 14,
                lastDeltaRed = 16,
                lastDeltaBlue = -16,
                redWins = 9,
                blueWins = 4,
                draws = 1,
                hudVisible = false
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<RankedProfile>(json);

            Assert.AreEqual(original.redElo, restored.redElo);
            Assert.AreEqual(original.blueElo, restored.blueElo);
            Assert.AreEqual(original.games, restored.games);
            Assert.AreEqual(original.lastDeltaRed, restored.lastDeltaRed);
            Assert.AreEqual(original.lastDeltaBlue, restored.lastDeltaBlue);
            Assert.AreEqual(original.redWins, restored.redWins);
            Assert.AreEqual(original.blueWins, restored.blueWins);
            Assert.AreEqual(original.draws, restored.draws);
            Assert.AreEqual(original.hudVisible, restored.hudVisible);
        }

        [Test]
        public void JsonSerialization_LegacyJsonWithoutDraws_LoadsWithZeroDraws()
        {
            string legacyJson = "{\"redElo\":1100,\"blueElo\":900,\"games\":5,\"lastDeltaRed\":16,\"lastDeltaBlue\":-16,\"redWins\":3,\"blueWins\":2,\"hudVisible\":true}";
            var profile = JsonUtility.FromJson<RankedProfile>(legacyJson);

            Assert.AreEqual(1100, profile.redElo);
            Assert.AreEqual(900, profile.blueElo);
            Assert.AreEqual(5, profile.games);
            Assert.AreEqual(3, profile.redWins);
            Assert.AreEqual(2, profile.blueWins);
            Assert.AreEqual(0, profile.draws);
            Assert.IsTrue(profile.hudVisible);
        }
    }
}
