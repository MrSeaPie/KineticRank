using System.Collections.Generic;
using System.Linq;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class PuzzlePack2Tests
    {
        static IEnumerable<string> Ids() => PuzzlePack2.All.Select(p => p.Id);

        [Test]
        public void Pack_HasTenPuzzlesWithUniqueIds()
        {
            Assert.AreEqual(10, PuzzlePack2.All.Count);
            CollectionAssert.AllItemsAreUnique(PuzzlePack2.All.Select(p => p.Id).ToList());
        }

        [Test]
        public void Pack_IdsDoNotCollideWithSeedsOrFirstPack()
        {
            var existing = PuzzleCatalog.Seeds().Select(s => s.Id).Concat(PuzzlePack.All.Select(p => p.Id)).ToList();
            foreach (var def in PuzzlePack2.All)
                CollectionAssert.DoesNotContain(existing, def.Id, def.Id);
        }

        [Test]
        public void GetById_FindsEachPuzzleAndMissesUnknown()
        {
            foreach (var def in PuzzlePack2.All)
                Assert.AreSame(def, PuzzlePack2.GetById(def.Id), def.Id);
            Assert.IsNull(PuzzlePack2.GetById("missing"));
        }

        [TestCaseSource(nameof(Ids))]
        public void Puzzle_HasExactlyOneWinningFirstMove(string id)
        {
            var def = PuzzlePack2.GetById(id);
            Assert.IsNotNull(def, id);
            Assert.IsFalse(def.Start.IsOver, id);
            var winners = PuzzleVerifier.WinningFirstMoves(def.Start, def.MovesN);
            Assert.AreEqual(1, winners.Count, id + " winners: " + string.Join(", ", winners));
        }

        // A two-move puzzle that also wins in one move would teach nothing
        [TestCaseSource(nameof(Ids))]
        public void Puzzle_IsNotSolvableInFewerMoves(string id)
        {
            var def = PuzzlePack2.GetById(id);
            for (int n = 1; n < def.MovesN; n++)
                Assert.IsFalse(PuzzleVerifier.HasForcedWin(def.Start, n), id + " wins in " + n);
        }
    }
}
