using System.Linq;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class PuzzleCatalogTests
    {
        [SetUp]
        public void SetUp() => PuzzleCatalog.ResetToSeeds();

        [TearDown]
        public void TearDown() => PuzzleCatalog.ResetToSeeds();

        static PuzzleEntry Entry(string id, int movesN = 1) =>
            new PuzzleEntry(id, "Title " + id, "Hint", PuzzleCatalog.Seeds().First().Start, movesN);

        [Test]
        public void Seeds_AreNonEmptyAndEachHasExactlyOneSolution()
        {
            var seeds = PuzzleCatalog.Seeds().ToList();
            Assert.IsNotEmpty(seeds);
            foreach (var seed in seeds)
            {
                Assert.IsFalse(seed.Start.IsOver, seed.Id);
                Assert.AreEqual(1, PuzzleVerifier.WinningFirstMoves(seed.Start, seed.MovesN).Count, seed.Id);
            }
        }

        [Test]
        public void All_StartsWithSeedsInOrder()
        {
            var ids = PuzzleCatalog.Seeds().Select(s => s.Id).ToList();
            CollectionAssert.AreEqual(ids, PuzzleCatalog.All.Select(e => e.Id).ToList());
        }

        [Test]
        public void Register_SkipsDuplicateIds()
        {
            var seedId = PuzzleCatalog.All[0].Id;
            var before = PuzzleCatalog.All.Count;

            var added = PuzzleCatalog.Register(new[] { Entry("extra"), Entry("extra"), Entry(seedId) });

            Assert.AreEqual(1, added);
            Assert.AreEqual(before + 1, PuzzleCatalog.All.Count);
            Assert.AreEqual("extra", PuzzleCatalog.All.Last().Id);
        }

        [Test]
        public void Register_RejectsInvalidEntries()
        {
            var before = PuzzleCatalog.All.Count;

            var added = PuzzleCatalog.Register(new[]
            {
                null,
                Entry(""),
                new PuzzleEntry("no_start", "t", "h", null, 1),
                Entry("zero_moves", 0),
                Entry("too_deep", PuzzleVerifier.MaxSupportedMoves + 1)
            });

            Assert.AreEqual(0, added);
            Assert.AreEqual(before, PuzzleCatalog.All.Count);
            Assert.AreEqual(0, PuzzleCatalog.Register(null));
        }

        [Test]
        public void TryGet_FindsRegisteredAndMissesUnknown()
        {
            PuzzleCatalog.Register(new[] { Entry("lookup") });

            Assert.IsTrue(PuzzleCatalog.TryGet("lookup", out var found));
            Assert.AreEqual("lookup", found.Id);
            Assert.IsFalse(PuzzleCatalog.TryGet("missing", out _));
            Assert.IsFalse(PuzzleCatalog.TryGet(null, out _));
        }

        [Test]
        public void ResetToSeeds_DropsLaterEntries()
        {
            PuzzleCatalog.Register(new[] { Entry("temp") });
            PuzzleCatalog.ResetToSeeds();

            Assert.IsFalse(PuzzleCatalog.TryGet("temp", out _));
            Assert.AreEqual(PuzzleCatalog.Seeds().Count(), PuzzleCatalog.All.Count);
        }
    }
}
