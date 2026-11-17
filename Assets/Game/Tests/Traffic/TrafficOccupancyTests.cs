using System;
using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficOccupancyTests
    {
        [Test]
        public void ASlowCarOnTheSameSide_BlocksALongStretch()
        {
            // Closing speed 30 - 12 = 18: the player passes (room = (4+4)/2 + 2 = 6 m) in 1/3 s, going 10 m... scaled by 30/18.
            double half = TrafficOccupancy.HalfLength(4.0, 4.0, 30.0, 12.0, 2.0, 0.0);

            Assert.AreEqual(6.0 * 30.0 / 18.0, half, 1e-9);
        }

        [Test]
        public void AnOncomingCar_BlocksAShorterStretchThanASlowOneOnTheSameSide()
        {
            double same = TrafficOccupancy.HalfLength(4.0, 4.0, 30.0, 15.0, 2.0, 0.25);
            double oncoming = TrafficOccupancy.HalfLength(4.0, 4.0, 30.0, -15.0, 2.0, 0.25);

            Assert.Less(oncoming, same);
            Assert.Less(oncoming, 6.0 + 0.25 * 45.0);
        }

        [Test]
        public void ALongerCarBlocksMore()
        {
            double car = TrafficOccupancy.HalfLength(3.8, 4.4, 30.0, 15.0, 2.0, 0.25);
            double truck = TrafficOccupancy.HalfLength(7.5, 4.4, 30.0, 15.0, 2.0, 0.25);

            Assert.Greater(truck, car);
        }

        [Test]
        public void ACarAsFastAsThePlayer_Throws()
        {
            Assert.Throws<ArgumentException>(() => TrafficOccupancy.HalfLength(4.0, 4.0, 30.0, 30.0, 2.0, 0.25));
        }
    }
}
