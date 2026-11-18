namespace WrongLine.Crash
{
    /// <summary>Where the player's box is at one moment: the run's time, the distance along the road and the position across it.</summary>
    public readonly struct PlayerBox
    {
        public readonly double Time;
        public readonly double Distance;
        public readonly float X;

        public PlayerBox(double time, double distance, float x)
        {
            Time = time;
            Distance = distance;
            X = x;
        }
    }
}
