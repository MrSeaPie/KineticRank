using System;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class MoveClockStateTests
    {
        [Test]
        public void InitialState_DefaultParameters_ConfiguredCorrectly()
        {
            var clock = new MoveClockState();

            Assert.AreEqual(30f, clock.TurnDuration);
            Assert.AreEqual(10f, clock.WarningThreshold);
            Assert.AreEqual(30f, clock.RemainingSeconds);
            Assert.IsFalse(clock.IsRunning);
            Assert.IsFalse(clock.IsPaused);
            Assert.IsFalse(clock.IsExpired);
            Assert.IsFalse(clock.IsWarning);
            Assert.AreEqual(1f, clock.FractionRemaining);
        }

        [Test]
        public void StartTurn_SetsRunningAndResetsTime()
        {
            var clock = new MoveClockState(30f, 10f);
            clock.StartTurn();

            Assert.IsTrue(clock.IsRunning);
            Assert.IsFalse(clock.IsPaused);
            Assert.AreEqual(30f, clock.RemainingSeconds);
            Assert.AreEqual(1f, clock.FractionRemaining);
        }

        [Test]
        public void Tick_WhileRunning_DrainsRemainingSeconds()
        {
            var clock = new MoveClockState(30f, 10f);
            clock.StartTurn();

            bool expired = clock.Tick(5f);

            Assert.IsFalse(expired);
            Assert.AreEqual(25f, clock.RemainingSeconds, 1e-4f);
            Assert.AreEqual(25f / 30f, clock.FractionRemaining, 1e-4f);
            Assert.IsFalse(clock.IsWarning);
        }

        [Test]
        public void Tick_ReachingWarningThreshold_ActivatesWarning()
        {
            var clock = new MoveClockState(30f, 10f);
            clock.StartTurn();

            clock.Tick(20f);

            Assert.AreEqual(10f, clock.RemainingSeconds, 1e-4f);
            Assert.IsTrue(clock.IsWarning);
            Assert.IsFalse(clock.IsExpired);
        }

        [Test]
        public void Tick_ExceedingDuration_ExpiresClock()
        {
            var clock = new MoveClockState(30f, 10f);
            clock.StartTurn();

            bool expired = clock.Tick(31f);

            Assert.IsTrue(expired);
            Assert.IsTrue(clock.IsExpired);
            Assert.AreEqual(0f, clock.RemainingSeconds);
            Assert.AreEqual(0f, clock.FractionRemaining);
            Assert.IsFalse(clock.IsWarning);
        }

        [Test]
        public void PauseAndResume_FreezesTickProgression()
        {
            var clock = new MoveClockState(30f, 10f);
            clock.StartTurn();

            clock.Tick(5f);
            Assert.AreEqual(25f, clock.RemainingSeconds, 1e-4f);

            clock.Pause();
            Assert.IsTrue(clock.IsPaused);

            bool expiredWhilePaused = clock.Tick(10f);
            Assert.IsFalse(expiredWhilePaused);
            Assert.AreEqual(25f, clock.RemainingSeconds, 1e-4f);

            clock.Resume();
            Assert.IsFalse(clock.IsPaused);

            clock.Tick(5f);
            Assert.AreEqual(20f, clock.RemainingSeconds, 1e-4f);
        }

        [Test]
        public void Reset_RestoresTurnDurationAndStopsClock()
        {
            var clock = new MoveClockState(30f, 10f);
            clock.StartTurn();
            clock.Tick(15f);

            clock.Reset();

            Assert.IsFalse(clock.IsRunning);
            Assert.IsFalse(clock.IsPaused);
            Assert.AreEqual(30f, clock.RemainingSeconds);
            Assert.AreEqual(1f, clock.FractionRemaining);
        }

        [Test]
        public void Constructor_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoveClockState(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoveClockState(-5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoveClockState(30f, -1f));
        }
    }
}
