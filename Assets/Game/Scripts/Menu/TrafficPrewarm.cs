using VContainer.Unity;
using WrongLine.Traffic;

namespace WrongLine.Menu
{
    /// <summary>Makes the traffic cars while the menu is up, so the run does not stall creating them.</summary>
    internal sealed class TrafficPrewarm : IStartable
    {
        private readonly ITrafficPool _pool;

        public TrafficPrewarm(ITrafficPool pool)
        {
            _pool = pool;
        }

        public void Start()
        {
            _pool.Prewarm();
        }
    }
}
