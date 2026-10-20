using System;

namespace KineticRank.Logic
{
    public enum RankTier { Bronze, Silver, Gold, Platinum, Diamond, Master }

    public static class RankBands
    {
        // Minimum ratings for division III of each tier. Bronze extends to the rating floor.
        public static readonly int[] TierMinimums = { 600, 900, 1200, 1500, 1800, 2100 };
        public const int DivisionWidth = 100;
        public static int OfflineMaximum => TierMinimums[(int)RankTier.Master] - 1;

        public static RankTier TierFor(int rating)
        {
            for (int i = TierMinimums.Length - 1; i > 0; i--)
                if (rating >= TierMinimums[i]) return (RankTier)i;
            return RankTier.Bronze;
        }

        // 3 = III, 2 = II, 1 = I; Master has no division (0).
        public static int DivisionFor(int rating, RankTier tier)
        {
            if (tier == RankTier.Master) return 0;
            int progress = Math.Max(0, rating - TierMinimums[(int)tier]);
            return 3 - Math.Min(2, progress / DivisionWidth);
        }
    }

    [Serializable]
    public class PlayerRankProfile
    {
        public const int PlacementCount = 5;
        public const int ProtectionDuration = 3;
        public int rating = 1000;
        public int gamesPlayed;
        public int placementGamesLeft = PlacementCount;
        public int peakRating = 1000;
        public int lastChange;
        public RankTier currentTier = RankTier.Silver;
        public int currentDivision = 2;
        public int promotionProtectionGamesLeft;
        public int season = 1;

        public bool IsPlacement => placementGamesLeft > 0;
        public string DisplayRank => IsPlacement
            ? "Placement " + (PlacementCount - placementGamesLeft) + "/" + PlacementCount
            : currentTier + (currentDivision == 0 ? "" : " " + (currentDivision == 3 ? "III" : currentDivision == 2 ? "II" : "I"));

        public void RecordMatch(int opponentRating, bool playerWon, bool offline = true)
        {
            RecordResult(opponentRating, playerWon ? 1.0 : 0.0, offline);
        }

        public void RecordDraw(int opponentRating, bool offline = true)
        {
            RecordResult(opponentRating, 0.5, offline);
        }

        public void RecordResult(int opponentRating, double score, bool offline = true)
        {
            if (score != 0.0 && score != 0.5 && score != 1.0)
                throw new ArgumentOutOfRangeException(nameof(score));
            if (opponentRating < EloCalculator.MinRating)
                throw new ArgumentOutOfRangeException(nameof(opponentRating));

            bool wasPlacement = IsPlacement;
            RankTier previousTier = currentTier;
            int k = wasPlacement ? 48 : EloCalculator.DefaultKFactor;
            double expected = 1.0 / (1.0 + Math.Pow(10.0, ((double)opponentRating - rating) / 400.0));
            int next = (int)Math.Round(rating + k * (score - expected));
            next = Math.Max(EloCalculator.MinRating, next);
            if (offline) next = Math.Min(RankBands.OfflineMaximum, next);
            lastChange = next - rating;
            rating = next;
            peakRating = Math.Max(peakRating, rating);
            gamesPlayed++;
            placementGamesLeft = Math.Max(0, PlacementCount - gamesPlayed);

            RankTier naturalTier = RankBands.TierFor(rating);
            // Placement ranks are hidden; revealing the initial rank is not a promotion.
            if (wasPlacement)
            {
                currentTier = naturalTier;
                promotionProtectionGamesLeft = 0;
            }
            else if (naturalTier > previousTier)
            {
                currentTier = naturalTier;
                promotionProtectionGamesLeft = ProtectionDuration;
            }
            else
            {
                currentTier = promotionProtectionGamesLeft > 0 && naturalTier < previousTier
                    ? previousTier : naturalTier;
                promotionProtectionGamesLeft = Math.Max(0, promotionProtectionGamesLeft - 1);
            }
            currentDivision = RankBands.DivisionFor(rating, currentTier);
        }
    }
}
