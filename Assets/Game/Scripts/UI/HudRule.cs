using System;

namespace WrongLine.UI
{
    /// <summary>What the HUD shows this frame.</summary>
    public readonly struct HudFrame
    {
        /// <summary>The run's distance in whole metres.</summary>
        public readonly int Meters;

        /// <summary>True when the text has to be rewritten (the whole metres changed since the last frame).</summary>
        public readonly bool Changed;

        /// <summary>True on the one frame the run passes the best run that was set before it began.</summary>
        public readonly bool NewRecord;

        public HudFrame(int meters, bool changed, bool newRecord)
        {
            Meters = meters;
            Changed = changed;
            NewRecord = newRecord;
        }
    }

    /// <summary>
    /// Decides when the HUD's distance text is rewritten (only when the whole metres change) and when "new record" is announced: once
    /// per run, when the run passes the best that stood before it. A first run (no best yet) never announces it.
    /// </summary>
    public sealed class HudRule
    {
        private readonly int _bestAtStart;
        private int _shown = -1;
        private bool _announced;

        public HudRule(int bestAtStart)
        {
            _bestAtStart = Math.Max(0, bestAtStart);
        }

        public HudFrame Update(double meters)
        {
            int whole = double.IsNaN(meters) || meters <= 0 ? 0 : (int)Math.Min(Math.Floor(meters), int.MaxValue);
            bool changed = whole != _shown;
            _shown = whole;

            bool record = false;
            if (!_announced && _bestAtStart > 0 && whole > _bestAtStart)
            {
                _announced = true;
                record = true;
            }

            return new HudFrame(whole, changed, record);
        }
    }
}
