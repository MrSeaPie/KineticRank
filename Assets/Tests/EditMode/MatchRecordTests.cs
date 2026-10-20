using System;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    public class MatchRecordTests
    {
        static MatchRecord Record(bool redStarts, params Move[] moves)
        {
            var record = new MatchRecord { RedStarts = redStarts, Plies = moves.Length };
            var state = GameState.FromGrid(new int[GameState.Columns, GameState.Rows], redStarts);
            foreach (var move in moves)
            {
                record.Moves.Add(new RecordedMove(move));
                state = state.Apply(move);
            }
            record.Result = state.Result;
            return record;
        }

        [Test]
        public void Replay_BlueStarterQueenAndCleaner_PreservesBoardAndResources()
        {
            var record = Record(false, new Move(0, MoveKind.Normal),
                new Move(0, MoveKind.Queen), new Move(0, MoveKind.Cleaner));
            var replay = record.Replay();
            for (int row = 0; row < GameState.Rows; row++)
                Assert.AreEqual(0, replay.Cell(0, row));
            Assert.IsFalse(replay.QueenAvailable(true));
            Assert.IsFalse(replay.CleanerAvailable(false));
            Assert.IsTrue(replay.QueenAvailable(false));
            Assert.IsTrue(replay.CleanerAvailable(true));
            Assert.IsTrue(replay.RedToMove);
            Assert.AreEqual("N1 Q1 C1", record.CompactNotation());
        }

        [Test]
        public void Replay_FullColumn_ShiftsBottomToken()
        {
            var moves = new Move[GameState.Rows + 1];
            for (int i = 0; i < moves.Length; i++) moves[i] = new Move(2, MoveKind.Normal);
            var replay = Record(true, moves).Replay();
            for (int row = 0; row < GameState.Rows; row++)
                Assert.AreEqual(row % 2 == 0 ? (int)CellOwner.Blue : (int)CellOwner.Red,
                    replay.Cell(2, row));
        }

        [Test]
        public void Replay_Timeout_ReconstructsNonterminalBoardWithoutInventingMove()
        {
            var record = Record(true, new Move(3, MoveKind.Normal));
            record.EndReason = MatchEndReason.Timeout;
            record.Result = Record(true, new Move(0, MoveKind.Normal), new Move(6, MoveKind.Normal),
                new Move(1, MoveKind.Normal), new Move(6, MoveKind.Normal),
                new Move(2, MoveKind.Normal), new Move(5, MoveKind.Normal),
                new Move(3, MoveKind.Normal)).Result;
            Assert.AreNotEqual(EvaluationResult.InProgress, record.Result);
            Assert.AreEqual(EvaluationResult.InProgress, record.Replay().Result);
            Assert.AreEqual(1, record.Moves.Count);
        }

        [Test]
        public void Replay_Traditional_DisablesBothPlayersPowerUps()
        {
            var record = Record(true);
            record.PowerUpsEnabled = false;
            var replay = record.Replay();
            Assert.IsFalse(replay.QueenAvailable(true));
            Assert.IsFalse(replay.QueenAvailable(false));
            Assert.IsFalse(replay.CleanerAvailable(true));
            Assert.IsFalse(replay.CleanerAvailable(false));
            record.Moves.Add(new RecordedMove(new Move(0, MoveKind.Queen)));
            record.Plies = 1;
            Assert.Throws<InvalidOperationException>(() => record.Replay());
        }

        [Test]
        public void Replay_RejectsUnsupportedVersionPlyMismatchAndIllegalMove()
        {
            var record = Record(true);
            record.FormatVersion++;
            Assert.Throws<NotSupportedException>(() => record.Replay());
            record.FormatVersion = MatchRecord.CurrentFormatVersion;
            record.RulesVersion++;
            Assert.Throws<NotSupportedException>(() => record.Replay());
            record.RulesVersion = MatchRecord.CurrentRulesVersion;
            record.Plies = 1;
            Assert.Throws<InvalidOperationException>(() => record.Replay());
            record.Moves.Add(new RecordedMove(new Move(-1, MoveKind.Normal)));
            Assert.Throws<InvalidOperationException>(() => record.Replay());
        }

        [Test]
        public void Replay_TerminalBoard_ValidatesOutcomeAndRejectsFurtherMoves()
        {
            var record = Record(true, new Move(0, MoveKind.Normal), new Move(6, MoveKind.Normal),
                new Move(1, MoveKind.Normal), new Move(6, MoveKind.Normal),
                new Move(2, MoveKind.Normal), new Move(5, MoveKind.Normal),
                new Move(3, MoveKind.Normal));
            Assert.IsTrue(record.Replay().IsOver);
            record.EndReason = MatchEndReason.Timeout;
            Assert.Throws<InvalidOperationException>(() => record.Replay());
            record.EndReason = MatchEndReason.BoardResult;
            record.Result = EvaluationResult.InProgress;
            Assert.Throws<InvalidOperationException>(() => record.Replay());
            record.Moves.Add(new RecordedMove(new Move(4, MoveKind.Normal)));
            record.Plies++;
            Assert.Throws<InvalidOperationException>(() => record.Replay());
        }

        [Test]
        public void UtcTimestamp_RoundTripsAndRejectsUnspecifiedTime()
        {
            var utc = new DateTime(2026, 10, 20, 21, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(utc, DateTime.Parse(MatchRecord.UtcTimestamp(utc), null,
                System.Globalization.DateTimeStyles.RoundtripKind));
            Assert.Throws<ArgumentException>(() => MatchRecord.UtcTimestamp(
                DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)));
        }
    }
}
