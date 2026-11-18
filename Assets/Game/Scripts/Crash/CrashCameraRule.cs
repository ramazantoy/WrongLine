using System;

namespace WrongLine.Crash
{
    /// <summary>
    /// Where along the road the camera stands after a crash: it keeps to the road like the chase camera but follows the wreck,
    /// forwards or back, so the wreck stays in the picture at the same angle. This is only the distance it follows with; the chase
    /// camera rule turns that into a position and a look, and keeps the wreck in view. Pure.
    /// </summary>
    public sealed class CrashCameraRule
    {
        private readonly CrashSettings.CameraGroup _settings;

        public CrashCameraRule(CrashSettings.CameraGroup settings, double startDistance)
        {
            _settings = settings;
            Distance = startDistance;
        }

        /// <summary>The distance along the road the camera is looking after, in metres.</summary>
        public double Distance { get; private set; }

        /// <param name="wreckDistance">Where the wreck is along the road.</param>
        /// <param name="deltaTime">0 (paused) leaves the camera where it is.</param>
        public void Update(double wreckDistance, float deltaTime)
        {
            double step = 1.0 - Math.Exp(-_settings.FollowSharpness * Math.Max(0f, deltaTime));
            Distance += (wreckDistance - Distance) * step;
        }
    }
}
