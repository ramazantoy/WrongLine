using WrongLine.Car;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>How long the player's car takes to change one lane, measured by running the real <see cref="LateralRule"/>. Pure.</summary>
    public static class LaneChangeMeasure
    {
        private const float StepSeconds = 1f / 120f;
        private const float SettledMeters = 0.1f;
        private const float GiveUpSeconds = 10f;

        /// <summary>Seconds from a drag of one lane width until the car is within 10 cm of the new lane's centre.</summary>
        public static float Seconds(CarSettings settings)
        {
            var rule = new LateralRule(settings);
            int lane = settings.Lateral.StartLane;
            int other = lane > 0 ? lane - 1 : lane + 1;
            float target = RoadSection.LaneCenterX(other);
            float drag = (target - rule.X) / settings.Lateral.DragMetersPerScreen;

            rule.Update(drag, 0f, StepSeconds);
            float time = StepSeconds;
            while (System.Math.Abs(rule.X - target) > SettledMeters && time < GiveUpSeconds)
            {
                rule.Update(0f, 0f, StepSeconds);
                time += StepSeconds;
            }

            return time;
        }
    }
}
