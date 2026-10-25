using System;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class DailyStreakTests
    {
        static int Day(int y, int m, int d) => DailyStreak.DayNumber(new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc));

        [Test]
        public void DayNumber_IgnoresTimeOfDay()
        {
            var start = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 10, 20, 23, 59, 59, DateTimeKind.Utc);
            Assert.AreEqual(DailyStreak.DayNumber(start), DailyStreak.DayNumber(end));
            Assert.AreEqual(DailyStreak.DayNumber(start) + 1, DailyStreak.DayNumber(end.AddSeconds(1)));
        }

        [Test]
        public void FirstSolve_StartsStreakAtOne()
        {
            var s = DailyStreak.ApplySolve(DailyStreakState.Empty, Day(2026, 10, 20));
            Assert.AreEqual(1, s.Current);
            Assert.AreEqual(1, s.Best);
            Assert.AreEqual(Day(2026, 10, 20), s.LastSolvedDay);
        }

        [Test]
        public void SameDayResolve_DoesNotDoubleCount()
        {
            var day = Day(2026, 10, 20);
            var once = DailyStreak.ApplySolve(DailyStreakState.Empty, day);
            var twice = DailyStreak.ApplySolve(once, day);
            Assert.AreEqual(once.Current, twice.Current);
            Assert.AreEqual(once.Best, twice.Best);
        }

        [Test]
        public void ConsecutiveDays_IncrementAcrossYearBoundary()
        {
            var s = DailyStreak.ApplySolve(DailyStreakState.Empty, Day(2026, 12, 30));
            s = DailyStreak.ApplySolve(s, Day(2026, 12, 31));
            s = DailyStreak.ApplySolve(s, Day(2027, 1, 1));
            Assert.AreEqual(3, s.Current);
            Assert.AreEqual(3, s.Best);
        }

        [Test]
        public void SkippedDay_ResetsCurrentButKeepsBest()
        {
            var s = DailyStreak.ApplySolve(DailyStreakState.Empty, Day(2026, 10, 1));
            s = DailyStreak.ApplySolve(s, Day(2026, 10, 2));
            s = DailyStreak.ApplySolve(s, Day(2026, 10, 4));
            Assert.AreEqual(1, s.Current);
            Assert.AreEqual(2, s.Best);
        }

        [Test]
        public void EarlierDayThanLastSolve_IsIgnored()
        {
            var s = DailyStreak.ApplySolve(DailyStreakState.Empty, Day(2026, 10, 20));
            var back = DailyStreak.ApplySolve(s, Day(2026, 10, 19));
            Assert.AreEqual(s.LastSolvedDay, back.LastSolvedDay);
            Assert.AreEqual(s.Current, back.Current);
        }

        [Test]
        public void CurrentAsOf_AliveThroughYesterday_ZeroAfterGap()
        {
            var s = DailyStreak.ApplySolve(DailyStreakState.Empty, Day(2026, 10, 19));
            s = DailyStreak.ApplySolve(s, Day(2026, 10, 20));
            Assert.AreEqual(2, DailyStreak.CurrentAsOf(s, Day(2026, 10, 20)));
            Assert.AreEqual(2, DailyStreak.CurrentAsOf(s, Day(2026, 10, 21)));
            Assert.AreEqual(0, DailyStreak.CurrentAsOf(s, Day(2026, 10, 22)));
            Assert.AreEqual(0, DailyStreak.CurrentAsOf(DailyStreakState.Empty, Day(2026, 10, 22)));
        }
    }
}
