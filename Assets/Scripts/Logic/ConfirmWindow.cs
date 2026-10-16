namespace KineticRank.Logic
{
    /// <summary>
    /// Two-press confirmation. The first Press arms the window; a second Press within
    /// windowSeconds confirms. Pure logic (no UnityEngine), so the caller supplies the clock.
    /// </summary>
    public sealed class ConfirmWindow
    {
        readonly float windowSeconds;
        float armedAt;
        bool armed;

        public ConfirmWindow(float windowSeconds)
        {
            this.windowSeconds = windowSeconds < 0f ? 0f : windowSeconds;
        }

        public bool IsArmed(float now) => armed && now - armedAt <= windowSeconds;

        // Returns true only when this press confirms an armed window.
        public bool Press(float now)
        {
            if (IsArmed(now))
            {
                armed = false;
                return true;
            }
            armed = true;
            armedAt = now;
            return false;
        }

        public void Cancel() => armed = false;
    }
}
