using System;

namespace KineticRank.Logic
{
    public class MoveClockState
    {
        public float Duration { get; set; }
        public float TurnDuration => Duration;
        public float WarningThreshold { get; set; }
        public float RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public bool ActiveIsRed { get; private set; }
        public bool IsExpired => RemainingSeconds <= 0f;
        public bool IsWarning => RemainingSeconds <= WarningThreshold && RemainingSeconds > 0f;
        public float FractionRemaining => Duration > 0f ? Math.Max(0f, Math.Min(1f, RemainingSeconds / Duration)) : 0f;

        public MoveClockState(float duration = 30f, float warningThreshold = 10f)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");
            if (warningThreshold < 0f) throw new ArgumentOutOfRangeException(nameof(warningThreshold), "Warning threshold cannot be negative.");

            Duration = duration;
            WarningThreshold = warningThreshold;
            RemainingSeconds = duration;
            IsRunning = false;
            IsPaused = false;
            ActiveIsRed = true;
        }

        public void StartTurn(bool isRed)
        {
            ActiveIsRed = isRed;
            RemainingSeconds = Duration;
            IsRunning = true;
            IsPaused = false;
        }

        public void StartTurn()
        {
            StartTurn(ActiveIsRed);
        }

        public void Reset()
        {
            RemainingSeconds = Duration;
            IsRunning = false;
            IsPaused = false;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public bool Tick(float dt)
        {
            if (!IsRunning || IsPaused || IsExpired) return false;
            RemainingSeconds -= dt;
            if (RemainingSeconds <= 0f)
            {
                RemainingSeconds = 0f;
                return true;
            }
            return false;
        }

        public void Update(float dt)
        {
            Tick(dt);
        }
    }
}
