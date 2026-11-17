using System;
using WrongLine.Car;

namespace WrongLine.Traffic.Tests
{
    internal static class TrafficFixtures
    {
        public static readonly TrafficModelSpec[] Models =
        {
            new TrafficModelSpec(3.8f, 1f),
            new TrafficModelSpec(4.5f, 1f),
            new TrafficModelSpec(5.2f, 0.6f),
            new TrafficModelSpec(7.5f, 0.4f),
        };

        public static TrafficSettings Settings()
        {
            var settings = new TrafficSettings();
            settings.Validate(60f, 400f);
            return settings;
        }

        public static TrafficPlanner Planner(ulong seed, TrafficSettings settings = null, CarSettings car = null)
        {
            car = car ?? new CarSettings();
            return new TrafficPlanner(Models, settings ?? Settings(), new PlayerSpeedProfile(car.Speed), seed, car.Lateral.StartLane, 8);
        }

        public static int ChangeMeters(TrafficSettings settings, CarSettings car)
        {
            return (int)Math.Ceiling(car.Speed.CruiseSpeed * settings.Layout.LaneChangeSeconds);
        }
    }
}
