using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    public class StyledGameRecorderTests
    {
        const int MaxPlies = 60;

        static IEnumerable<IReadOnlyList<Move>> SomeOpenings()
        {
            return StyledSelfPlay.TwoPlyOpenings().Where((opening, i) => i % 9 == 0).Take(6);
        }

        [Test]
        public void Record_MatchesPlayGame_ForSameOpeningStylesAndSeed()
        {
            foreach (var opening in SomeOpenings())
            {
                foreach (int? seed in new int?[] { null, 7 })
                {
                    var game = StyledSelfPlay.PlayGame(opening, BotStyle.Attacker, BotStyle.Default, 1, 1, seed, MaxPlies);
                    var record = StyledGameRecorder.Record(opening, BotStyle.Attacker, BotStyle.Default, 1, 1, seed, MaxPlies);

                    Assert.AreEqual(game.Result, record.Result);
                    Assert.AreEqual(game.Plies, record.Plies);
                    Assert.AreEqual(game.Capped, record.Capped);
                    Assert.AreEqual(game.RedStyle, record.RedStyle);
                    Assert.AreEqual(game.BlueStyle, record.BlueStyle);
                }
            }
        }

        [Test]
        public void Record_MovesStartWithOpening_AndPowerUpPliesPointAtThoseMoves()
        {
            foreach (var opening in SomeOpenings())
            {
                var record = StyledGameRecorder.Record(opening, BotStyle.Default, BotStyle.Attacker, 1, 1, 3, MaxPlies);

                Assert.AreEqual(record.Plies, record.Moves.Count);
                Assert.AreEqual(opening.Count, record.OpeningPlies);
                for (int i = 0; i < opening.Count; i++) Assert.AreEqual(opening[i], record.Moves[i]);

                int queens = record.Moves.Count(m => m.Kind == MoveKind.Queen);
                int cleaners = record.Moves.Count(m => m.Kind == MoveKind.Cleaner);
                Assert.AreEqual(queens, record.RedQueenPlies.Count + record.BlueQueenPlies.Count);
                Assert.AreEqual(cleaners, record.RedCleanerPlies.Count + record.BlueCleanerPlies.Count);

                // Red moves on odd plies, Blue on even plies
                foreach (int ply in record.RedQueenPlies) { Assert.AreEqual(MoveKind.Queen, record.Moves[ply - 1].Kind); Assert.AreEqual(1, ply % 2); }
                foreach (int ply in record.RedCleanerPlies) { Assert.AreEqual(MoveKind.Cleaner, record.Moves[ply - 1].Kind); Assert.AreEqual(1, ply % 2); }
                foreach (int ply in record.BlueQueenPlies) { Assert.AreEqual(MoveKind.Queen, record.Moves[ply - 1].Kind); Assert.AreEqual(0, ply % 2); }
                foreach (int ply in record.BlueCleanerPlies) { Assert.AreEqual(MoveKind.Cleaner, record.Moves[ply - 1].Kind); Assert.AreEqual(0, ply % 2); }
            }
        }

        [Test]
        public void Record_DrawKindAndFinalMove_AgreeWithResult()
        {
            foreach (var opening in SomeOpenings())
            {
                var record = StyledGameRecorder.Record(opening, BotStyle.Default, BotStyle.Default, 1, 1, null, MaxPlies);

                if (record.HasWinner)
                {
                    Assert.AreEqual(StyledDrawKind.None, record.DrawKind);
                    Assert.AreEqual(record.Moves[record.Moves.Count - 1].Kind, record.FinalMoveKind);
                }
                else
                {
                    Assert.IsNull(record.FinalMoveKind);
                    Assert.IsFalse(record.WinnerUsedQueen);
                    Assert.IsFalse(record.WinnerUsedCleaner);
                    Assert.AreEqual(record.Capped ? StyledDrawKind.Cap : StyledDrawKind.Rules, record.DrawKind);
                }
            }
        }

        [Test]
        public void Record_MaxPliesEqualToOpening_IsCapDrawWithOnlyTheOpening()
        {
            var opening = SomeOpenings().First();
            var record = StyledGameRecorder.Record(opening, BotStyle.Default, BotStyle.Default, 1, 1, null, opening.Count);

            Assert.IsTrue(record.Capped);
            Assert.AreEqual(StyledDrawKind.Cap, record.DrawKind);
            Assert.AreEqual(opening.Count, record.Plies);
        }

        [Test]
        public void RecordAll_UsesSeedBasePlusIndex_LikeRun()
        {
            var openings = SomeOpenings().ToList();
            var report = StyledSelfPlay.Run(openings, BotStyle.Default, BotStyle.Attacker, 1, 1, 100, MaxPlies);
            var records = StyledGameRecorder.RecordAll(openings, BotStyle.Default, BotStyle.Attacker, 1, 1, 100, MaxPlies);

            Assert.AreEqual(report.Games.Count, records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                Assert.AreEqual(100 + i, records[i].Seed);
                Assert.AreEqual(report.Games[i].Result, records[i].Result);
                Assert.AreEqual(report.Games[i].Plies, records[i].Plies);
            }
        }

        [Test]
        public void CsvRow_HasOneCellPerHeaderColumn()
        {
            var record = StyledGameRecorder.Record(SomeOpenings().First(), BotStyle.Default, BotStyle.Default, 1, 1, 5, MaxPlies);
            int headerCells = StyledGameRecord.CsvHeader.Split(',').Length;
            Assert.AreEqual(headerCells, record.ToCsvRow().Split(',').Length);
        }

        [Test]
        public void Record_NullArguments_Throw()
        {
            var opening = SomeOpenings().First();
            Assert.Throws<ArgumentNullException>(() => StyledGameRecorder.Record(null, BotStyle.Default, BotStyle.Default, 1, 1, null, MaxPlies));
            Assert.Throws<ArgumentNullException>(() => StyledGameRecorder.Record(opening, null, BotStyle.Default, 1, 1, null, MaxPlies));
            Assert.Throws<ArgumentNullException>(() => StyledGameRecorder.Record(opening, BotStyle.Default, null, 1, 1, null, MaxPlies));
            Assert.Throws<ArgumentOutOfRangeException>(() => StyledGameRecorder.Record(opening, BotStyle.Default, BotStyle.Default, 1, 1, null, opening.Count - 1));
        }
    }
}
