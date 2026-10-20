using System;
using UnityEngine;

namespace KineticRank.Logic
{
    // Streak state for the daily puzzle. Days are UTC day numbers so every player shares the same boundary.
    public struct DailyStreakState
    {
        public int LastSolvedDay; // -1 when never solved
        public int Current;
        public int Best;

        public static DailyStreakState Empty => new DailyStreakState { LastSolvedDay = -1 };
    }

    // Daily puzzle streak. Kept apart from rank, Elo, solo ladder and puzzle_solved_ keys
    // so daily results never affect competitive data.
    public static class DailyStreak
    {
        public const string LastDayKey = "daily_last_solved_day";
        public const string CurrentKey = "daily_streak_current";
        public const string BestKey = "daily_streak_best";

        // Unspecified kind is treated as UTC; local times are converted first
        public static int DayNumber(DateTime date)
        {
            if (date.Kind == DateTimeKind.Local) date = date.ToUniversalTime();
            return (int)(date.Date.Ticks / TimeSpan.TicksPerDay);
        }

        // Same-day re-solve is a no-op, the next day extends the streak, a gap restarts it at 1.
        // A day earlier than the last solve (clock moved back) is ignored so it can't farm streaks.
        public static DailyStreakState ApplySolve(DailyStreakState state, int day)
        {
            if (state.LastSolvedDay >= 0 && day <= state.LastSolvedDay) return state;
            var current = state.LastSolvedDay >= 0 && day == state.LastSolvedDay + 1 ? state.Current + 1 : 1;
            return new DailyStreakState
            {
                LastSolvedDay = day,
                Current = current,
                Best = Math.Max(state.Best, current)
            };
        }

        // Streak to show today: still alive if the last solve was today or yesterday, otherwise 0
        public static int CurrentAsOf(DailyStreakState state, int today)
        {
            if (state.LastSolvedDay < 0) return 0;
            return today - state.LastSolvedDay <= 1 ? state.Current : 0;
        }

        public static DailyStreakState Load()
        {
            return new DailyStreakState
            {
                LastSolvedDay = PlayerPrefs.GetInt(LastDayKey, -1),
                Current = PlayerPrefs.GetInt(CurrentKey, 0),
                Best = PlayerPrefs.GetInt(BestKey, 0)
            };
        }

        public static void Save(DailyStreakState state)
        {
            PlayerPrefs.SetInt(LastDayKey, state.LastSolvedDay);
            PlayerPrefs.SetInt(CurrentKey, state.Current);
            PlayerPrefs.SetInt(BestKey, state.Best);
            PlayerPrefs.Save();
        }

        public static DailyStreakState RecordSolve(DateTime utcNow)
        {
            var state = ApplySolve(Load(), DayNumber(utcNow));
            Save(state);
            return state;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(LastDayKey);
            PlayerPrefs.DeleteKey(CurrentKey);
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.Save();
        }
    }
}
