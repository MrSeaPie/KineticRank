using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class DailyPuzzleTests
    {
        [SetUp]
        public void SetUp() => PuzzleCatalog.ResetToSeeds();

        [TearDown]
        public void TearDown() => PuzzleCatalog.ResetToSeeds();

        static List<PuzzleEntry> FakeEntries(int n)
        {
            var start = PuzzleCatalog.Seeds().First().Start;
            var list = new List<PuzzleEntry>();
            for (var i = 0; i < n; i++) list.Add(new PuzzleEntry("fake_" + i, "Fake " + i, "", start, 1));
            return list;
        }

        [Test]
        public void Fnv1a_MatchesPublishedVectors()
        {
            // Standard 32-bit FNV-1a test vectors, so the hash cannot drift between builds
            Assert.AreEqual(2166136261u, DailyPuzzle.Fnv1a(""));
            Assert.AreEqual(0xE40C292Cu, DailyPuzzle.Fnv1a("a"));
            Assert.AreEqual(0xBF9CF968u, DailyPuzzle.Fnv1a("foobar"));
        }

        [Test]
        public void DateKey_IsInvariantAndIgnoresTimeOfDay()
        {
            Assert.AreEqual("2026-10-20", DailyPuzzle.DateKey(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual("2026-10-20", DailyPuzzle.DateKey(new DateTime(2026, 10, 20, 23, 59, 59, DateTimeKind.Utc)));
            Assert.AreEqual("2026-10-21", DailyPuzzle.DateKey(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void ForDate_SameDayGivesSamePuzzle()
        {
            var morning = new DateTime(2026, 10, 20, 1, 0, 0, DateTimeKind.Utc);
            var night = new DateTime(2026, 10, 20, 22, 0, 0, DateTimeKind.Utc);
            var a = DailyPuzzle.ForDate(morning);
            Assert.IsNotNull(a);
            Assert.AreSame(a, DailyPuzzle.ForDate(night));
            Assert.AreSame(a, DailyPuzzle.ForDate(morning));
        }

        [Test]
        public void ForDate_CoversEveryEntryOverAYear()
        {
            var entries = FakeEntries(7);
            var seen = new HashSet<string>();
            var day = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 365; i++) seen.Add(DailyPuzzle.ForDate(day.AddDays(i), entries).Id);
            Assert.AreEqual(entries.Count, seen.Count);
        }

        [Test]
        public void ForDate_EmptyOrNullListReturnsNull()
        {
            var day = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsNull(DailyPuzzle.ForDate(day, new List<PuzzleEntry>()));
            Assert.IsNull(DailyPuzzle.ForDate(day, null));
        }

        [Test]
        public void Index_RejectsNonPositiveCount()
        {
            var day = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPuzzle.Index(day, 0));
        }
    }
}
