namespace WrongLine.Haptics
{
    /// <summary>
    /// Decides whether a vibration plays: never when haptics are off; a pulse as hard as or lighter than the last one is dropped if it
    /// comes sooner than the minimum gap after it (a slider dragged or a button hammered does not buzz without end); a harder one always plays.
    /// </summary>
    public sealed class HapticRule
    {
        private readonly float _minInterval;
        private HapticKind _lastKind;
        private float _lastTime = float.NegativeInfinity;

        public HapticRule(float minInterval)
        {
            _minInterval = minInterval;
        }

        public bool Allow(HapticKind kind, float now, bool enabled)
        {
            if (!enabled)
            {
                return false;
            }

            if (kind <= _lastKind && now - _lastTime < _minInterval)
            {
                return false;
            }

            _lastKind = kind;
            _lastTime = now;
            return true;
        }
    }
}
