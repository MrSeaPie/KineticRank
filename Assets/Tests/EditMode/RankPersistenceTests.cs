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
            Assert.AreEqual(-16, profile.lastDeltaRed);
            Assert.AreEqual(16, profile.lastDeltaBlue);
            Assert.AreEqual(984, profile.redElo);
            Assert.AreEqual(1016, profile.blueElo);
        }

        [Test]
        public void RecordMatch_RatingFloor_NeverDropsBelowMinimumRating()
        {
            var profile = new RankedProfile { redElo = 10, blueElo = 10 };
            profile.RecordMatch(redWon: false, kFactor: 32);

            Assert.AreEqual(EloCalculator.MinRating, profile.redElo);
            Assert.GreaterOrEqual(profile.redElo, 1);

            var blueProfile = new RankedProfile { redElo = 10, blueElo = 10 };
            blueProfile.RecordMatch(redWon: true, kFactor: 32);

            Assert.AreEqual(EloCalculator.MinRating, blueProfile.blueElo);
            Assert.GreaterOrEqual(blueProfile.blueElo, 1);
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
                blueWins = 4,
                hudVisible = false
            };

            profile.ResetRanked(preserveHud: true);

            Assert.AreEqual(1000, profile.redElo);
            Assert.AreEqual(1000, profile.blueElo);
            Assert.AreEqual(0, profile.games);
            Assert.AreEqual(0, profile.redWins);
            Assert.AreEqual(0, profile.blueWins);
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
                hudVisible = false
            };

            profile.ResetRanked(preserveHud: false);

            Assert.AreEqual(1000, profile.redElo);
            Assert.AreEqual(1000, profile.blueElo);
            Assert.IsTrue(profile.hudVisible);
        }

        [Test]
        public void JsonSerialization_RoundTrip_MaintainsStateIntegrity()
        {
            var original = new RankedProfile
            {
                redElo = 1184,
                blueElo = 916,
                games = 25,
                lastDeltaRed = 14,
                lastDeltaBlue = -14,
                redWins = 17,
                blueWins = 8,
                hudVisible = false
            };

            string json = JsonUtility.ToJson(original, prettyPrint: true);
            var restored = JsonUtility.FromJson<RankedProfile>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.redElo, restored.redElo);
            Assert.AreEqual(original.blueElo, restored.blueElo);
            Assert.AreEqual(original.games, restored.games);
            Assert.AreEqual(original.lastDeltaRed, restored.lastDeltaRed);
            Assert.AreEqual(original.lastDeltaBlue, restored.lastDeltaBlue);
            Assert.AreEqual(original.redWins, restored.redWins);
            Assert.AreEqual(original.blueWins, restored.blueWins);
            Assert.AreEqual(original.hudVisible, restored.hudVisible);
        }

        [Test]
        public void JsonSerialization_LegacyJsonWithoutHudVisible_PreservesParsedValues()
        {
            string legacyJson = "{\"redElo\":1120,\"blueElo\":880,\"games\":10,\"lastDeltaRed\":12,\"lastDeltaBlue\":-12,\"redWins\":7,\"blueWins\":3}";
            var restored = JsonUtility.FromJson<RankedProfile>(legacyJson);

            Assert.IsNotNull(restored);
            Assert.AreEqual(1120, restored.redElo);
            Assert.AreEqual(880, restored.blueElo);
            Assert.AreEqual(10, restored.games);
            Assert.AreEqual(7, restored.redWins);
            Assert.AreEqual(3, restored.blueWins);
        }
    }
}
