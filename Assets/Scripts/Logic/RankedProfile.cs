using System;

namespace KineticRank.Logic
{
    [Serializable]
    public class RankedProfile
    {
        public int redElo = 1000;
        public int blueElo = 1000;
        public int games = 0;
        public int lastDeltaRed = 0;
        public int lastDeltaBlue = 0;
        public int redWins = 0;
        public int blueWins = 0;
        public bool hudVisible = true;

        public void RecordMatch(bool redWon, int kFactor = EloCalculator.DefaultKFactor)
        {
            EloCalculator.CalculateMatch(redElo, blueElo, redWon, out int newRed, out int newBlue, out int deltaRed, out int deltaBlue, kFactor);
            lastDeltaRed = deltaRed;
            lastDeltaBlue = deltaBlue;
            redElo = newRed;
            blueElo = newBlue;
            games += 1;
            if (redWon) redWins += 1; else blueWins += 1;
        }

        public void ResetRanked(bool preserveHud = true)
        {
            bool keepHud = hudVisible;
            redElo = 1000;
            blueElo = 1000;
            games = 0;
            lastDeltaRed = 0;
            lastDeltaBlue = 0;
            redWins = 0;
            blueWins = 0;
            hudVisible = preserveHud ? keepHud : true;
        }
    }
}
