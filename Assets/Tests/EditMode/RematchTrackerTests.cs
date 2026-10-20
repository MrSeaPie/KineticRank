using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class RematchTrackerTests
    {
        [SetUp]
        public void SetUp()
        {
            RematchTracker.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            RematchTracker.Reset();
        }

        [Test]
        public void InitialState_StartsAtMatchZero_RedMovesFirst()
        {
            Assert.AreEqual(0, RematchTracker.RematchIndex);
            Assert.IsTrue(RematchTracker.CurrentFirstMoverIsRed);
        }

        [Test]
        public void RegisterRematch_AlternatesFirstMover()
        {
            // Initial match (index 0): Red first
            Assert.IsTrue(RematchTracker.CurrentFirstMoverIsRed);

            // Rematch 1: Blue first
            RematchTracker.RegisterRematch();
            Assert.AreEqual(1, RematchTracker.RematchIndex);
            Assert.IsFalse(RematchTracker.CurrentFirstMoverIsRed);

            // Rematch 2: Red first
            RematchTracker.RegisterRematch();
            Assert.AreEqual(2, RematchTracker.RematchIndex);
            Assert.IsTrue(RematchTracker.CurrentFirstMoverIsRed);

            // Rematch 3: Blue first
            RematchTracker.RegisterRematch();
            Assert.AreEqual(3, RematchTracker.RematchIndex);
            Assert.IsFalse(RematchTracker.CurrentFirstMoverIsRed);
        }

        [Test]
        public void Reset_RestoresMatchZeroAndRedFirst()
        {
            RematchTracker.RegisterRematch();
            Assert.IsFalse(RematchTracker.CurrentFirstMoverIsRed);

            RematchTracker.Reset();
            Assert.AreEqual(0, RematchTracker.RematchIndex);
            Assert.IsTrue(RematchTracker.CurrentFirstMoverIsRed);
        }

        [Test]
        public void FirstMoverIsRedForRematch_PureFunctionMatchesAlternation()
        {
            Assert.IsTrue(RematchTracker.FirstMoverIsRedForRematch(0));
            Assert.IsFalse(RematchTracker.FirstMoverIsRedForRematch(1));
            Assert.IsTrue(RematchTracker.FirstMoverIsRedForRematch(2));
            Assert.IsFalse(RematchTracker.FirstMoverIsRedForRematch(3));
            Assert.IsTrue(RematchTracker.FirstMoverIsRedForRematch(100));
            Assert.IsFalse(RematchTracker.FirstMoverIsRedForRematch(101));
        }

        [Test]
        public void SetRematchIndex_ClampsNegativeValuesToZero()
        {
            RematchTracker.SetRematchIndex(-5);
            Assert.AreEqual(0, RematchTracker.RematchIndex);
            Assert.IsTrue(RematchTracker.CurrentFirstMoverIsRed);
        }
    }
}
