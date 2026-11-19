using System;

namespace WrongLine.UI
{
    /// <summary>The best run, in whole metres, kept in PlayerPrefs.</summary>
    public sealed class BestDistance : IBestDistance
    {
        private const string BEST_KEY = "records.best_meters";

        private readonly IPrefsStore _store;

        public BestDistance(IPrefsStore store)
        {
            _store = store;
            // A damaged or hand-edited value must not leave a negative record.
            Meters = Math.Max(0, _store.GetInt(BEST_KEY, 0));
        }

        public int Meters { get; private set; }

        public bool Submit(double meters)
        {
            // Whole metres rounded down: the screen shows what the player really covered.
            int whole = double.IsNaN(meters) || meters <= 0 ? 0 : (int)Math.Min(Math.Floor(meters), int.MaxValue);
            if (whole <= Meters)
            {
                return false;
            }

            Meters = whole;
            _store.SetInt(BEST_KEY, whole);
            return true;
        }
    }
}
