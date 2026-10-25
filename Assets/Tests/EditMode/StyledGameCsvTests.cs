using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    public class StyledGameCsvTests
    {
        const int MaxPlies = 60;

        static IEnumerable<IReadOnlyList<Move>> SomeOpenings()
        {
            return StyledSelfPlay.TwoPlyOpenings().Where((opening, i) => i % 9 == 0).Take(6);
        }

        [Test]
        public void Header_IsPinned()
        {
            Assert.AreEqual("opening,red_depth,blue_depth,max_repeats,red_style,blue_style,seed,result,plies,draw_kind,final_move_kind,red_queen_plies,red_cleaner_plies,blue_queen_plies,blue_cleaner_plies,winner_used_queen,winner_used_cleaner", StyledGameCsv.Header);
        }

        [Test]
        public void Row_PrependsOpeningDepthsAndMaxRepeats_ToRecordRow()
        {
            int columns = StyledGameCsv.Header.Split(',').Length;
            foreach (var opening in SomeOpenings())
            {
                foreach (int? seed in new int?[] { null, 7 })
                {
                    var game = StyledSelfPlay.PlayGame(opening, BotStyle.Attacker, BotStyle.Default, 1, 1, seed, MaxPlies);
                    var record = StyledGameRecorder.Record(opening, BotStyle.Attacker, BotStyle.Default, 1, 1, seed, MaxPlies);
                    var cells = StyledGameCsv.Row(game, record, 1, 1).Split(',');

                    Assert.AreEqual(columns, cells.Length);
                    CollectionAssert.AreEqual(opening, StyledGameCsv.ParseOpening(cells[0]));
                    Assert.AreEqual("1", cells[1]);
                    Assert.AreEqual("1", cells[2]);
                    Assert.AreEqual(game.MaxRepeats.ToString(), cells[3]);
                    Assert.AreEqual(record.ToCsvRow(), string.Join(",", cells.Skip(4)));
                }
            }
        }

        [Test]
        public void PlayRow_EqualsRowOfSeparatelyPlayedGame()
        {
            foreach (var opening in SomeOpenings())
            {
                var game = StyledSelfPlay.PlayGame(opening, BotStyle.Default, BotStyle.Attacker, 1, 1, 3, MaxPlies);
                var record = StyledGameRecorder.Record(opening, BotStyle.Default, BotStyle.Attacker, 1, 1, 3, MaxPlies);

                Assert.AreEqual(StyledGameCsv.Row(game, record, 1, 1), StyledGameCsv.PlayRow(opening, BotStyle.Default, BotStyle.Attacker, 1, 1, 3, MaxPlies));
            }
        }

        [Test]
        public void Row_RecordOfDifferentGame_Throws()
        {
            var opening = SomeOpenings().First();
            var game = StyledSelfPlay.PlayGame(opening, BotStyle.Default, BotStyle.Default, 1, 1, null, MaxPlies);
            var record = StyledGameRecorder.Record(opening, BotStyle.Attacker, BotStyle.Default, 1, 1, null, MaxPlies);

            Assert.Throws<ArgumentException>(() => StyledGameCsv.Row(game, record, 1, 1));
        }

        [Test]
        public void OpeningCell_RoundTrips_EveryTwoPlyOpening()
        {
            foreach (var opening in StyledSelfPlay.TwoPlyOpenings())
            {
                string cell = StyledGameCsv.OpeningCell(opening);
                StringAssert.DoesNotContain(",", cell);
                CollectionAssert.AreEqual(opening, StyledGameCsv.ParseOpening(cell));
            }
            Assert.AreEqual("", StyledGameCsv.OpeningCell(new Move[0]));
            Assert.AreEqual(0, StyledGameCsv.ParseOpening("").Count);
            Assert.AreEqual("Normal@3;Queen@2", StyledGameCsv.OpeningCell(new[] { new Move(3, MoveKind.Normal), new Move(2, MoveKind.Queen) }));
        }

        [Test]
        public void ParseOpening_BadCell_Throws()
        {
            Assert.Throws<FormatException>(() => StyledGameCsv.ParseOpening("Normal3"));
            Assert.Throws<FormatException>(() => StyledGameCsv.ParseOpening("Bishop@3"));
            Assert.Throws<FormatException>(() => StyledGameCsv.ParseOpening("Normal@x"));
        }
    }
}
