using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using KineticRank.Logic;

public class MoveClockRuntimeTests
{
    GameObject clockObj;
    MoveClock clock;
    GameObject overlayObj;
    HowToPlayOverlay overlay;

    [SetUp]
    public void SetUp()
    {
        MoveClock.ForceRankedForTesting = true;
        clockObj = new GameObject("MoveClockTest");
        clock = clockObj.AddComponent<MoveClock>();
        clock.turnDuration = 30f;
        clock.warningThreshold = 10f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        MoveClock.ForceRankedForTesting = false;
        if (clockObj) Object.Destroy(clockObj);
        if (overlayObj) Object.Destroy(overlayObj);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Countdown_WithHelpClosed_DrainsClockAndUpdatesVisuals()
    {
        clock.StartTurn(true);
        Assert.That(clock.ActiveIsRed, Is.True);
        Assert.That(clock.State.RemainingSeconds, Is.EqualTo(30f));

        yield return new WaitForSeconds(0.15f);

        Assert.That(clock.State.RemainingSeconds, Is.LessThan(30f));
        Assert.That(clock.State.DrainFraction, Is.LessThan(1f));
        if (clock.redTimerBar != null)
            Assert.That(clock.redTimerBar.fillAmount, Is.LessThan(1f));
    }

    [UnityTest]
    public IEnumerator OverlayOpen_PausesClock_AndResumeContinuesCountdown()
    {
        overlayObj = new GameObject("OverlayTest");
        overlay = overlayObj.AddComponent<HowToPlayOverlay>();
        overlay.Open();
        Assert.That(overlay.IsOpen, Is.True);

        clock.StartTurn(true);
        float initialRemaining = clock.State.RemainingSeconds;

        yield return new WaitForSeconds(0.1f);

        Assert.That(clock.State.RemainingSeconds, Is.EqualTo(initialRemaining), "Clock must remain paused while overlay IsOpen is true.");

        overlay.Close();
        Assert.That(overlay.IsOpen, Is.False);

        yield return new WaitForSeconds(0.15f);

        Assert.That(clock.State.RemainingSeconds, Is.LessThan(initialRemaining), "Clock must resume countdown after overlay is closed.");
    }

    [Test]
    public void Timeout_TriggersOnceOnly_AndFlagsSettled()
    {
        clock.StartTurn(true);
        clock.State.Tick(30.1f);

        Assert.That(clock.State.IsExpired, Is.True);
        clock.TriggerTimeoutForfeit(true);
        Assert.That(clock.SettledTimeout, Is.True);
        Assert.That(clock.MatchOver, Is.True);

        Assert.DoesNotThrow(() => clock.TriggerTimeoutForfeit(true));
    }

    [Test]
    public void NonRankedMatch_RemainsInactive()
    {
        MoveClock.ForceRankedForTesting = false;
        Assert.That(MoveClock.IsRankedMatch, Is.False);
        clock.StartTurn(true);
        Assert.That(clock.State.RemainingSeconds, Is.EqualTo(30f));
    }
}
