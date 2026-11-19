using System;

namespace WrongLine.UI
{
    /// <summary>The number that counts up on the game over screen: from 0 to the target in a set time, fast at first and settling.</summary>
    public static class CountUpRule
    {
        public static int Value(int target, float elapsed, float seconds)
        {
            if (target <= 0)
            {
                return 0;
            }

            if (seconds <= 0f || elapsed >= seconds)
            {
                return target;
            }

            float t = Math.Max(0f, elapsed) / seconds;
            float eased = 1f - (1f - t) * (1f - t) * (1f - t); // Ease-out cubic.
            return Math.Min(target, (int)(target * eased));
        }
    }
}
