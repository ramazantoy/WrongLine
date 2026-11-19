namespace WrongLine.UI
{
    /// <summary>When the game over distance ticks as it counts up: at most once per interval, and only on a frame where the number moved. Pure.</summary>
    public sealed class CountTickRule
    {
        private readonly float _interval;
        private float _since;

        public CountTickRule(float interval)
        {
            _interval = interval;
            _since = interval; // The first change ticks at once.
        }

        public bool Step(float deltaTime, bool changed)
        {
            _since += deltaTime;
            if (!changed || _since < _interval)
            {
                return false;
            }

            _since = 0f;
            return true;
        }
    }
}
