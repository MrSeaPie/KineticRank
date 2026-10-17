using System;

namespace KineticRank.Logic
{
    public static class EloCalculator
    {
        public const int DefaultKFactor = 32;
        public const int MinRating = 1;

        public static float ExpectedScore(int ratingA, int ratingB)
        {
            float exponent = (ratingB - ratingA) / 400f;
            return 1f / (1f + (float)Math.Pow(10.0, exponent));
        }

        public static void CalculateMatch(int redElo, int blueElo, bool redWon, out int newRed, out int newBlue, out int deltaRed, out int deltaBlue, int kFactor = DefaultKFactor)
        {
            float expRed = ExpectedScore(redElo, blueElo);
            float expBlue = ExpectedScore(blueElo, redElo);

            float sRed = redWon ? 1f : 0f;
            float sBlue = redWon ? 0f : 1f;

            int unconstrainedRed = (int)Math.Round(redElo + kFactor * (sRed - expRed));
            int unconstrainedBlue = (int)Math.Round(blueElo + kFactor * (sBlue - expBlue));

            newRed = Math.Max(MinRating, unconstrainedRed);
            newBlue = Math.Max(MinRating, unconstrainedBlue);

            deltaRed = newRed - redElo;
            deltaBlue = newBlue - blueElo;
        }

        public static void CalculateDraw(int redElo, int blueElo, out int newRed, out int newBlue, out int deltaRed, out int deltaBlue, int kFactor = DefaultKFactor)
        {
            float expRed = ExpectedScore(redElo, blueElo);
            float expBlue = ExpectedScore(blueElo, redElo);

            float sRed = 0.5f;
            float sBlue = 0.5f;

            int unconstrainedRed = (int)Math.Round(redElo + kFactor * (sRed - expRed));
            int unconstrainedBlue = (int)Math.Round(blueElo + kFactor * (sBlue - expBlue));

            newRed = Math.Max(MinRating, unconstrainedRed);
            newBlue = Math.Max(MinRating, unconstrainedBlue);

            deltaRed = newRed - redElo;
            deltaBlue = newBlue - blueElo;
        }
    }
}
