using System;

namespace KineticRank.Logic
{
    public class MoveClockState
    {
        public const float DefaultTurnDuration = 30f;
        public const float DefaultWarningThreshold = 10f;

        public float TurnDuration { get; }
        public float WarningThreshold { get; }
        public float RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        public bool IsExpired => RemainingSeconds <= 0f;
        public bool IsWarning => IsRunning && RemainingSeconds <= WarningThreshold && RemainingSeconds > 0f;
        public float FractionRemaining => TurnDuration > 0f ? Math.Max(0f, Math.Min(1f, RemainingSeconds / TurnDuration)) : 0f;

        public MoveClockState(float turnDuration = DefaultTurnDuration, float warningThreshold = DefaultWarningThreshold)
        {
            if (turnDuration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(turnDuration), "Turn duration must be greater than zero.");
            if (warningThreshold < 0f)
                throw new ArgumentOutOfRangeException(nameof(warningThreshold), "Warning threshold cannot be negative.");

            TurnDuration = turnDuration;
            WarningThreshold = warningThreshold;
            RemainingSeconds = turnDuration;
            IsRunning = false;
            IsPaused = false;
        }

        public void StartTurn()
        {
            RemainingSeconds = TurnDuration;
            IsRunning = true;
            IsPaused = false;
        }

        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }

        public void Pause()
        {
            if (IsRunning)
            {
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsRunning)
            {
                IsPaused = false;
            }
        }

        public void Reset()
        {
            RemainingSeconds = TurnDuration;
            IsRunning = false;
            IsPaused = false;
        }

        public bool Tick(float deltaSeconds)
        {
            if (!IsRunning || IsPaused || IsExpired)
                return false;

            if (deltaSeconds > 0f)
            {
                RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaSeconds);
            }

            return IsExpired;
        }
    }
}
