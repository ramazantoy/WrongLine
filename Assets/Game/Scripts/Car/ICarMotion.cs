namespace WrongLine.Car
{
    /// <summary>Where the car is across the road and how fast it goes, as the road run and the views read it each frame.</summary>
    internal interface ICarMotion
    {
        /// <summary>Metres per second along the road.</summary>
        float Speed { get; }

        /// <summary>Metres covered along the road since the run began (it stops growing at a crash).</summary>
        double Distance { get; }

        /// <summary>Metres right of the road's centre line.</summary>
        float X { get; }

        float TargetX { get; }

        /// <summary>Sideways speed, metres per second.</summary>
        float Velocity { get; }

        /// <summary>-1 left, 0 middle, 1 right.</summary>
        int Lane { get; }

        SignalDirection Signal { get; }

        bool LampOn { get; }

        /// <summary>True once the car has crashed: it no longer moves.</summary>
        bool Crashed { get; }
    }
}
