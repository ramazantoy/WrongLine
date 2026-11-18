using System;

namespace WrongLine.Crash
{
    /// <summary>
    /// What the game does between a crash and the game over screen: it slows down to a share of normal speed, then the screen
    /// comes up. Counts real seconds (the caller divides the game's frame time by the game's speed), so slow motion does not
    /// stretch it, and a pause, whose frame time is 0, holds it. Pure.
    /// </summary>
    public sealed class CrashTimelineRule
    {
        private readonly CrashSettings.SlowMotionGroup _settings;

        public CrashTimelineRule(CrashSettings.SlowMotionGroup settings)
        {
            _settings = settings;
        }

        /// <summary>Real seconds since the crash.</summary>
        public float Elapsed { get; private set; }

        /// <summary>The game's speed now, 1 at the crash and easing to the slow-motion scale.</summary>
        public float Scale { get; private set; } = 1f;

        /// <summary>True from <see cref="CrashSettings.SlowMotionGroup.GameOverSeconds"/> on.</summary>
        public bool GameOverDue => Elapsed >= _settings.GameOverSeconds;

        public void Update(float realDeltaTime)
        {
            Elapsed += Math.Max(0f, realDeltaTime);
            float ease = _settings.EaseSeconds > 0f ? Math.Min(1f, Elapsed / _settings.EaseSeconds) : 1f;
            Scale = 1f + (_settings.Scale - 1f) * ease;
        }
    }
}
